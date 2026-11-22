# Code signing

Releases are signed with a self-signed certificate, whose public part is
[werfer-code-signing.cer](werfer-code-signing.cer):

- Subject: `CN=Werfer, O=moonpyk, DC=net`
- SHA-1 thumbprint: `BD9DF4E1159FD0F8AD96C9B4748479830D276980`
- Valid until November 22, 2027

To trust it across a domain, an administrator can deploy it by Group Policy in both **Trusted Root Certification
Authorities** and **Trusted Publishers**. Before deploying it, check that its thumbprint matches the signature of a
downloaded `werfer-win-Setup.exe` (Properties, Digital Signatures).
