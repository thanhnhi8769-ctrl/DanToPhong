using System.Net.Sockets;
using System.Security.Authentication;
using System.Threading.Channels;
using MailKit;
using MailKit.Net.Smtp;

namespace DanToPhong.Server;

internal sealed class EmailVerificationQueue
{
    private const int MaxAttempts = 3;
    private readonly Channel<EmailCodeJob> _channel;
    private readonly SemaphoreSlim _capacitySlots;
    private readonly Func<EmailCodeJob, CancellationToken, Task> _sendCode;
    private readonly TimeSpan _retryDelay;
    private long _nextJobId;

    internal EmailVerificationQueue(
        int capacity = 64,
        Func<EmailCodeJob, CancellationToken, Task>? sendCode = null,
        TimeSpan? retryDelay = null)
    {
        if (capacity < 1)
            throw new ArgumentOutOfRangeException(nameof(capacity));

        _channel = Channel.CreateBounded<EmailCodeJob>(new BoundedChannelOptions(capacity)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = false,
            AllowSynchronousContinuations = false
        });
        _capacitySlots = new SemaphoreSlim(capacity, capacity);
        _sendCode = sendCode ?? ((job, token) =>
            EmailVerificationService.SendCodeAsync(job.Email, job.Code, job.PasswordReset, token));
        _retryDelay = retryDelay ?? TimeSpan.FromSeconds(2);
        if (_retryDelay < TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(retryDelay));
    }

    internal bool TryReserve(out Reservation? reservation)
    {
        reservation = null;
        if (_channel.Reader.Completion.IsCompleted || !_capacitySlots.Wait(0))
            return false;

        if (_channel.Reader.Completion.IsCompleted)
        {
            _capacitySlots.Release();
            return false;
        }

        reservation = new Reservation(this);
        return true;
    }

    internal void Complete() => _channel.Writer.TryComplete();

    internal async Task RunAsync(CancellationToken token)
    {
        await foreach (EmailCodeJob job in _channel.Reader.ReadAllAsync(token).ConfigureAwait(false))
        {
            try
            {
                await DeliverWithRetryAsync(job, token).ConfigureAwait(false);
            }
            finally
            {
                _capacitySlots.Release();
            }
        }
    }

    private async Task DeliverWithRetryAsync(EmailCodeJob job, CancellationToken token)
    {
        for (int attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            try
            {
                await _sendCode(job, token).ConfigureAwait(false);
                Console.WriteLine($"[SMTP] Email job {job.Id} delivered.");
                return;
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                throw;
            }
            catch (SmtpCommandException ex) when (IsRetryableStatus(ex.StatusCode))
            {
                if (attempt == MaxAttempts)
                {
                    Console.Error.WriteLine($"[SMTP] Email job {job.Id} failed after {attempt} attempts (status {(int)ex.StatusCode}).");
                    return;
                }

                Console.Error.WriteLine($"[SMTP] Email job {job.Id} received status {(int)ex.StatusCode}; retrying ({attempt}/{MaxAttempts}).");
            }
            catch (SmtpCommandException)
            {
                Console.Error.WriteLine($"[SMTP] Email job {job.Id} failed with a non-retryable SMTP response.");
                return;
            }
            catch (OperationCanceledException) when (!token.IsCancellationRequested)
            {
                if (attempt == MaxAttempts)
                {
                    Console.Error.WriteLine($"[SMTP] Email job {job.Id} timed out after {attempt} attempts.");
                    return;
                }

                Console.Error.WriteLine($"[SMTP] Email job {job.Id} timed out; retrying ({attempt}/{MaxAttempts}).");
            }
            catch (Exception ex) when (IsRetryableTransportFailure(ex))
            {
                if (attempt == MaxAttempts)
                {
                    Console.Error.WriteLine($"[SMTP] Email job {job.Id} failed after {attempt} transport attempts ({ex.GetType().Name}).");
                    return;
                }

                Console.Error.WriteLine($"[SMTP] Email job {job.Id} encountered {ex.GetType().Name}; retrying ({attempt}/{MaxAttempts}).");
            }
            catch (Exception ex) when (
                ex is AuthenticationException || ex is MailKit.Security.AuthenticationException)
            {
                Console.Error.WriteLine($"[SMTP] Email job {job.Id} failed TLS authentication; not retrying.");
                return;
            }
            catch (InvalidOperationException)
            {
                Console.Error.WriteLine($"[SMTP] Email job {job.Id} rejected because SMTP configuration is invalid.");
                return;
            }
            catch (FormatException)
            {
                Console.Error.WriteLine($"[SMTP] Email job {job.Id} rejected because the recipient address is invalid.");
                return;
            }
            catch (ArgumentException)
            {
                Console.Error.WriteLine($"[SMTP] Email job {job.Id} rejected because the message configuration is invalid.");
                return;
            }
            catch (CommandException)
            {
                Console.Error.WriteLine($"[SMTP] Email job {job.Id} failed with a non-retryable MailKit error.");
                return;
            }

            await Task.Delay(_retryDelay, token).ConfigureAwait(false);
        }
    }

    private static bool IsRetryableStatus(SmtpStatusCode statusCode) =>
        (int)statusCode is >= 400 and < 600;

    private static bool IsRetryableTransportFailure(Exception exception) =>
        exception is IOException or SocketException or TimeoutException or ProtocolException;

    private bool Commit(EmailCodeJob job)
    {
        if (!_channel.Writer.TryWrite(job))
        {
            _capacitySlots.Release();
            return false;
        }
        return true;
    }

    private void ReleaseSlot() => _capacitySlots.Release();

    internal sealed class Reservation : IDisposable
    {
        private readonly EmailVerificationQueue _owner;
        private int _state;

        internal Reservation(EmailVerificationQueue owner) => _owner = owner;

        internal void Enqueue(string email, string code, bool passwordReset)
        {
            if (Interlocked.CompareExchange(ref _state, 1, 0) != 0)
                throw new InvalidOperationException("Email queue reservation has already been used.");

            try
            {
                var job = new EmailCodeJob(
                    Interlocked.Increment(ref _owner._nextJobId), email, code, passwordReset);
                if (!_owner.Commit(job))
                    throw new InvalidOperationException("Reserved email queue capacity was unavailable.");
            }
            catch (InvalidOperationException)
            {
                Interlocked.Exchange(ref _state, 2);
                throw;
            }
        }

        public void Dispose()
        {
            if (Interlocked.CompareExchange(ref _state, 2, 0) == 0)
                _owner.ReleaseSlot();
        }
    }

    internal sealed record EmailCodeJob(long Id, string Email, string Code, bool PasswordReset);
}
