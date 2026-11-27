# Internet demo runbook

This is a manual checklist, not evidence that an AWS deployment was performed.
No AWS CLI command or resource change is authorized by this document. AWS
resources can incur charges; first confirm the account's current Free Tier,
pricing, and resource state in the AWS console. Stop if the owner has not
approved the test.

## Before the demo

- Confirm the owner approved the date, duration, budget, region, account, and
  source public IPv4 address. Re-check the `/32` immediately before changing
  ingress because home, school, VPN, and mobile IPs can differ.
- Verify current EC2, RDS, EIP, and Security Group state. Record which EIP is
  associated with the RDS network interface and do not release or move it.
- Check the expected charge and Free Tier eligibility for EC2, RDS, public IPv4,
  storage, snapshots, and data transfer. Do not rely on an earlier estimate.
- Confirm SQL Server 1433 is not publicly reachable. Prepare the database and
  credentials through the approved secret mechanism; do not put credentials in
  this repository or screenshots.
- Use a trusted certificate for the public hostname. Confirm the hostname is
  in its SAN and agree on its SHA-256 leaf-certificate pin before updating any
  client `server.json`. Do not turn off TLS or certificate validation.
- Prepare sanitized test accounts, smtp4dev/SMTP configuration if permitted,
  a rollback plan, a shutdown time, and a second client on a different network.

## Start and verify

1. In the AWS console, start RDS and wait until it reports `available`.
2. Start the intended EC2 instance and wait until it reports `running` and its
   health checks pass. Confirm the intended public hostname/address and do not
   change or release the RDS-associated EIP.
3. Add only the temporary inbound TCP `9000` rule from the verified tester
   address as a single `/32`. Do not use `0.0.0.0/0`; do not open database port
   `1433`.
4. Deploy the reviewed Server build using the approved procedure. Configure its
   environment file outside Git with restricted permissions, verify database
   connectivity, and confirm the TLS listener uses the expected certificate.
5. Update each test client's untracked `server.json`: public hostname, port
   `9000`, `useTls: true`, and the agreed
   `certificateThumbprintSha256`. Do not use a raw untrusted self-signed
   certificate for an Internet demo.
6. From the two independent networks, record the UTC/local time and run only
   the approved smoke cases: TLS connection, login/OTP, create/join room,
   snapshot observation, disconnect, and a clean client exit. Save sanitized
   evidence and label every case PASS, PARTIAL, or BLOCKED from observed
   results only.

## Shut down

1. Remove the temporary TCP `9000` `/32` ingress rule and verify it is absent.
2. Stop EC2 and wait for `stopped`; then stop RDS and verify its final state.
   Do not stop RDS first. Confirm no unintended instances, rules, or public
   addresses remain changed.
3. Record final resource states, elapsed runtime, any charges shown, evidence
   paths, and follow-up actions. Escalate unexpected state instead of guessing.

## Troubleshooting

| Symptom | Check | Safe response |
|---|---|---|
| TCP connection times out | Verify the public address, Server listener, instance health, and temporary source `/32` rule. | Reconfirm the tester's current public IP; never widen ingress to all IPv4. Remove the rule after the test. |
| TLS/pin error | Compare configured hostname/SAN, certificate validity, and the agreed leaf SHA-256 pin. | Stop the demo and coordinate a correct certificate/config update. Do not disable validation. |
| Login works but OTP does not arrive | Check server SMTP configuration, queue/worker logs, provider status, and the approved test inbox. | Keep credentials private; do not paste OTPs into logs or screenshots. Mark the case BLOCKED if delivery is not observed. |
| Server cannot reach SQL Server | Verify RDS state, endpoint, database, network routing, and least-privilege rules. | Do not expose port 1433 publicly; correct the approved private path or stop. |
| A room or snapshot does not appear | Check both client logs, room state, TCP stream, and whether the client joined/spectated. | Record the exact observed failure; do not claim gameplay success from a successful TLS connection. |
