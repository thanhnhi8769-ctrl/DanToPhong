#!/usr/bin/env python3
"""Credential-free TCP/TLS probe for an operator-owned DanToPhong test server."""

import argparse
import concurrent.futures
import json
import os
import socket
import ssl
import statistics
import struct
import sys
import threading
import time

MAX_FRAME = 64 * 1024
PING = json.dumps({"command": "PING"}, separators=(",", ":")).encode("utf-8")


def make_context():
    ca_file = os.environ.get("DANTOPHONG_CA_CERT")
    return ssl.create_default_context(cafile=ca_file)


def connect(host, port, tls, server_name, timeout):
    raw = socket.create_connection((host, port), timeout=timeout)
    raw.settimeout(timeout)
    if not tls:
        return raw
    try:
        return make_context().wrap_socket(raw, server_hostname=server_name or host)
    except Exception:
        raw.close()
        raise


def read_exact(stream, length):
    chunks = bytearray()
    while len(chunks) < length:
        data = stream.recv(length - len(chunks))
        if not data:
            raise EOFError("connection closed before complete frame")
        chunks.extend(data)
    return bytes(chunks)


def send_frame(stream, payload):
    if not payload or len(payload) > MAX_FRAME:
        raise ValueError("payload length outside allowed frame size")
    stream.sendall(struct.pack("!I", len(payload)) + payload)


def receive_json(stream):
    length = struct.unpack("!I", read_exact(stream, 4))[0]
    if length < 1 or length > MAX_FRAME:
        raise ValueError("server response length outside allowed frame size")
    return json.loads(read_exact(stream, length).decode("utf-8"))


def one_client(args, barrier):
    started = time.perf_counter()
    stream = connect(args.host, args.port, not args.plaintext, args.server_name, args.timeout)
    try:
        if barrier:
            barrier.wait(timeout=args.timeout)
        send_frame(stream, PING)
        response = receive_json(stream)
        elapsed_ms = (time.perf_counter() - started) * 1000
        valid = response.get("success") is False and response.get("code") == "ERR_UNAUTHORIZED"
        return valid, elapsed_ms
    finally:
        stream.close()


def run_load(args, count):
    barrier = threading.Barrier(count)
    with concurrent.futures.ThreadPoolExecutor(max_workers=count) as pool:
        futures = [pool.submit(one_client, args, barrier) for _ in range(count)]
        results = []
        failures = 0
        for future in concurrent.futures.as_completed(futures):
            try:
                valid, elapsed = future.result()
                results.append(elapsed)
                failures += not valid
            except Exception:
                failures += 1
    if results:
        print(
            f"clients={count} responses={len(results)} failures={failures} "
            f"mean_ms={statistics.mean(results):.1f} max_ms={max(results):.1f}"
        )
    else:
        print(f"clients={count} responses=0 failures={failures}")
    return failures == 0 and len(results) == count


def run_slow_sender(args):
    stream = connect(args.host, args.port, not args.plaintext, args.server_name, args.timeout)
    try:
        frame = struct.pack("!I", len(PING)) + PING
        for byte in frame:
            stream.sendall(bytes((byte,)))
            time.sleep(args.byte_delay)
        response = receive_json(stream)
        passed = response.get("success") is False and response.get("code") == "ERR_UNAUTHORIZED"
        print(f"slow_sender={'PASS' if passed else 'FAIL'}")
        return passed
    finally:
        stream.close()


def run_oversize(args):
    stream = connect(args.host, args.port, not args.plaintext, args.server_name, args.timeout)
    try:
        stream.sendall(struct.pack("!I", MAX_FRAME + 1))
        try:
            response = stream.recv(1)
            passed = response == b""
        except (ConnectionResetError, ssl.SSLError, socket.timeout):
            passed = True
        print(f"oversize_rejected={'PASS' if passed else 'FAIL'}")
        return passed
    finally:
        stream.close()


def run_plaintext_rejection(args):
    stream = connect(args.host, args.port, False, args.server_name, args.timeout)
    try:
        try:
            send_frame(stream, PING)
            response = receive_json(stream)
            passed = response.get("success") is not True
        except (EOFError, ConnectionResetError, ssl.SSLError, OSError, ValueError):
            passed = True
        print(f"plaintext_rejected={'PASS' if passed else 'FAIL'}")
        return passed
    finally:
        stream.close()


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--host", default="localhost")
    parser.add_argument("--port", type=int, default=9000)
    parser.add_argument("--server-name", help="TLS certificate hostname; defaults to --host")
    parser.add_argument("--clients", default="10,50", help="comma-separated concurrent client counts")
    parser.add_argument("--timeout", type=float, default=10)
    parser.add_argument("--byte-delay", type=float, default=0.02)
    parser.add_argument("--plaintext", action="store_true", help="disable TLS only for isolated fake-data tests")
    parser.add_argument("--slow-sender", action="store_true")
    parser.add_argument("--oversize", action="store_true")
    parser.add_argument("--check-plaintext-rejection", action="store_true")
    args = parser.parse_args()
    if args.host.lower() not in {"localhost", "127.0.0.1", "::1"}:
        parser.error("this protocol/load probe is restricted to loopback addresses")
    if args.server_name and args.server_name.lower() not in {"localhost", "127.0.0.1", "::1"}:
        parser.error("--server-name must also be a loopback name")
    passed = True

    if args.check_plaintext_rejection:
        passed &= run_plaintext_rejection(args)
    else:
        for count in (int(value) for value in args.clients.split(",")):
            if count < 1 or count > 128:
                parser.error("client count must be between 1 and 128")
            passed &= run_load(args, count)
        if args.slow_sender:
            passed &= run_slow_sender(args)
        if args.oversize:
            passed &= run_oversize(args)

    return 0 if passed else 1


if __name__ == "__main__":
    sys.exit(main())
