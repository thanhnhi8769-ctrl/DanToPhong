# Internet demo runbook

The canonical consolidated report is
[BAO_CAO_HOAN_CHINH_DAN_TO_PHONG.md](../BAO_CAO_HOAN_CHINH_DAN_TO_PHONG.md).

This runbook prepares the deployment workflow; it is not evidence that AWS
resources were deployed. A read-only AWS snapshot is included in the canonical
report. AWS resources can incur charges. First
confirm current Free Tier eligibility, pricing, profile, region, account, and
resource state. Do not start/stop resources or change ingress without explicit
owner approval.

## Before the demo

- Confirm the owner approved the date, duration, budget, region, account, and
  source public IPv4 address. Re-check the `/32` immediately before changing
  ingress because home, school, VPN, and mobile IPs can differ.
- Run the read-only status helper after AWS credentials are valid. Verify EC2,
  RDS, EIP, and Security Group state; record which EIP is associated with the
  RDS network interface and do not release or move it. If AWS reports an
  invalid/expired token, stop and refresh the approved profile; do not guess a
  different account or region.
- Check the expected charge and Free Tier eligibility for EC2, RDS, public IPv4,
  storage, snapshots, and data transfer. Do not rely on an earlier estimate.
- Confirm SQL Server 1433 is not publicly reachable. Prepare the database and
  credentials through the approved secret mechanism; do not put credentials in
  this repository or screenshots.
- Keep `/etc/dantophong/server.env` mode `0600`, owned by root.
  The PFX at `DANTOPHONG_CERT_PATH` must be outside Git and readable only by
  root and the `ubuntu` service account; never make the private key world-readable.
- Use a stable DNS hostname where possible. EC2 public IPv4/DNS can change after
  stop/start; a certificate issued for the old address will not match a new one.
- Prepare sanitized test accounts, smtp4dev/SMTP configuration if permitted,
  a rollback plan, a shutdown time, and a second client on a different network.

## One-time EC2 deployment

The supplied unit/setup script assumes an Ubuntu EC2 image with the `ubuntu`
account, systemd, `/usr/bin/dotnet`, and .NET 10 runtime already installed.
It checks these prerequisites and fails rather than installing software or
changing the host package configuration. Confirm the AMI before using it.

1. Publish the framework-dependent Linux Server on the development machine:

   ```powershell
   $publish = Join-Path $env:TEMP 'dtp-server-publish'
   dotnet publish .\UI\DanToPhong.Server\DanToPhong.Server.csproj -c Release -r linux-x64 --self-contained false -o $publish
   ```

2. Set the current EC2 SSH endpoint, approved key path, and the same stable
   certificate/client DNS name that both clients will use, then copy only the
   published binaries and deployment scripts:

   ```powershell
   $ec2Host = 'REPLACE_WITH_EC2_PUBLIC_DNS'
   $certificateHost = 'YOUR_STABLE_DNS_NAME'
   $sshKey = 'C:\path\outside\repo\to\ssh-key.pem'
   ssh -i $sshKey "ubuntu@$ec2Host" 'mkdir -p /tmp/dtp-deploy'
   scp -i $sshKey -r $publish "ubuntu@${ec2Host}:/tmp/"
   scp -i $sshKey .\deploy\aws\setup-ec2.sh .\deploy\aws\dantophong.service "ubuntu@${ec2Host}:/tmp/dtp-deploy/"
   ```

   Then copy the publish contents and run setup (after the env file is prepared):

   ```powershell
   $remoteSetup = 'sudo install -d -o ubuntu -g ubuntu /opt/dantophong && sudo cp -a /tmp/dtp-server-publish/. /opt/dantophong/ && sudo bash /tmp/dtp-deploy/setup-ec2.sh --certificate-host "' + $certificateHost + '"'
   ssh -i $sshKey "ubuntu@$ec2Host" $remoteSetup
   ```

   Do not copy any local secrets, PFX, or `server.env`.
3. Configure `/etc/dantophong/server.env` on EC2 using the approved secret
   mechanism. Start from `deploy/aws/server.env.example`; replace every
   placeholder, use the private RDS endpoint/database, set TLS true and port
   9000, and do not add a secondary listener. Keep it root-only.
4. Once the final DNS name or current EC2 IP is known, run on EC2:

   ```bash
sudo bash /tmp/dtp-deploy/setup-ec2.sh --certificate-host "YOUR_STABLE_DNS_NAME_OR_IP"
   ```

   The script checks the Linux .NET 10 runtime, database configuration,
   certificate SAN/validity, and published DLL; creates a self-signed PFX
   outside Git if needed; prints the SHA-256 thumbprint; installs/enables the
   systemd unit; restarts the service; and verifies `systemctl` plus TCP 9000.
   It refuses to replace an existing mismatched/expired certificate unless
   `--rotate-certificate` is explicitly supplied. The unit uses
   `Restart=always`, `WantedBy=multi-user.target`, and
   `/etc/dantophong/server.env`.

## Start and verify

After confirming the current account/region/resource state and obtaining
approval for charges, the local helper provides a bounded start sequence. Set
the current tester's SSH key path locally; do not put it in the repository.

```powershell
.\scripts\aws-test-helper.ps1 -Action Status
```

Before using `Start`, in the Security Group allow TCP 9000 only from each
approved tester's current public IPv4 as a separate `/32` ingress source. For
TC02 with two networks, authorize both exact `/32` addresses. Never use
`0.0.0.0/0` or `::/0`; do not expose 1433. RDS ingress should accept 1433 only
from the Server's Security Group/private path. The helper does not create,
edit, or remove rules.

Then start and verify:

```powershell
.\scripts\aws-test-helper.ps1 -Action Start -RdsInstanceId 'REPLACE_WITH_RDS_ID' -Ec2InstanceId 'REPLACE_WITH_EC2_ID' -ClientHostName 'YOUR_STABLE_DNS_NAME' -SshKeyPath 'C:\path\outside\repo\to\ssh-key.pem'
```

The helper asks for an exact confirmation, waits for RDS `available` before
starting EC2, waits for EC2 `running` and passing health checks, checks over
SSH that `dantophong` is active/enabled and TCP 9000 listens, then tests TCP
9000 from the current client network. It never changes Security Groups,
Elastic IPs, DNS, or database configuration. Waits are bounded to 30 minutes
by default. Use `-Region`/`-Profile` only when those are the approved current
values.

1. Ensure both clients use the same stable DNS name in `server.json`. If using
   an IP, it must be the current EC2 address and present as an IP SAN. If the
   address changed, regenerate the certificate explicitly and repin.
2. Copy the thumbprint printed by `setup-ec2.sh` on EC2, then verify the live
   endpoint from the local machine and emit the client JSON:

   ```powershell
   .\scripts\aws-test-helper.ps1 -Action Pin -ClientHostName 'YOUR_STABLE_DNS_NAME' -ExpectedThumbprint 'COPY_64_HEX_THUMBPRINT_FROM_EC2'
   ```

   The helper refuses hostname mismatch, expired/non-self-signed certificates,
   unexpected chain errors, or a thumbprint mismatch. It does not disable
   validation in the application. Put the resulting values only in each
   untracked client `server.json`.
3. From both independent networks, record the local time and run TC02. Capture
   TLS connection, login/OTP, room create/join, and snapshot behavior; save
   sanitized evidence and label outcomes only from observed results.

## Shut down

1. Remove the temporary TCP `9000` `/32` ingress rules and verify they are
   absent; never change the RDS-associated EIP.
2. After owner approval, run the helper below. It requires exact confirmation,
   stops EC2 and waits for `stopped`, then stops RDS and waits for `stopped`:

   ```powershell
.\scripts\aws-test-helper.ps1 -Action Stop -RdsInstanceId 'REPLACE_WITH_RDS_ID' -Ec2InstanceId 'REPLACE_WITH_EC2_ID'
   ```

3. Run `-Action Status` and record final states, elapsed runtime, any charges
   shown, evidence paths, and follow-up actions. Escalate unexpected state
   instead of guessing.

## Troubleshooting

| Symptom | Check | Safe response |
|---|---|---|
| TCP connection times out | Verify the public address, Server listener, instance health, and temporary source `/32` rule. | Reconfirm the tester's current public IP; never widen ingress to all IPv4. Remove the rule after the test. |
| TLS/pin error | Compare configured hostname/SAN, certificate validity, and the agreed leaf SHA-256 pin. | Stop the demo and coordinate a correct certificate/config update. Do not disable validation. |
| Login works but OTP does not arrive | Check server SMTP configuration, queue/worker logs, provider status, and the approved test inbox. | Keep credentials private; do not paste OTPs into logs or screenshots. Mark the case BLOCKED if delivery is not observed. |
| Server cannot reach SQL Server | Verify RDS state, endpoint, database, network routing, and least-privilege rules. | Do not expose port 1433 publicly; correct the approved private path or stop. |
| A room or snapshot does not appear | Check both client logs, room state, TCP stream, and whether the client joined/spectated. | Record the exact observed failure; do not claim gameplay success from a successful TLS connection. |
