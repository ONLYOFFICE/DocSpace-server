// (c) Copyright Ascensio System SIA 2009-2026
//
// This program is a free software product.
// You can redistribute it and/or modify it under the terms
// of the GNU Affero General Public License (AGPL) version 3 as published by the Free Software
// Foundation. In accordance with Section 7(a) of the GNU AGPL its Section 15 shall be amended
// to the effect that Ascensio System SIA expressly excludes the warranty of non-infringement of
// any third-party rights.
//
// This program is distributed WITHOUT ANY WARRANTY, without even the implied warranty
// of MERCHANTABILITY or FITNESS FOR A PARTICULAR  PURPOSE. For details, see
// the GNU AGPL at: http://www.gnu.org/licenses/agpl-3.0.html
//
// You can contact Ascensio System SIA at Lubanas st. 125a-25, Riga, Latvia, EU, LV-1021.
//
// The  interactive user interfaces in modified source and object code versions of the Program must
// display Appropriate Legal Notices, as required under Section 5 of the GNU AGPL version 3.
//
// Pursuant to Section 7(b) of the License you must retain the original Product logo when
// distributing the program. Pursuant to Section 7(e) we decline to grant you any rights under
// trademark law for use of our trademarks.
//
// All the Product's GUI elements, including illustrations and icon sets, as well as technical writing
// content are licensed under the terms of the Creative Commons Attribution-ShareAlike 4.0
// International. See the License terms at http://creativecommons.org/licenses/by-sa/4.0/legalcode

using System.Diagnostics;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace ASC.AppHost.Configuration;

public static class DevCertificateGenerator
{
    private const string Subject = "CN=docspace.localhost";
    private static readonly string[] _dnsNames = ["docspace.localhost", "localhost"];

    public static (string CertDir, string CrtFileName, string KeyFileName) EnsureCertificate(string appHostDirectory)
    {
        var certDir = Path.Combine(appHostDirectory, "certs");
        Directory.CreateDirectory(certDir);

        const string crtFileName = "docspace.localhost.crt";
        const string keyFileName = "docspace.localhost.key";

        var crtPath = Path.Combine(certDir, crtFileName);
        var keyPath = Path.Combine(certDir, keyFileName);

        var trustMarkerPath = Path.Combine(certDir, ".trusted");

        if (File.Exists(crtPath) && File.Exists(keyPath))
        {
            var existing = X509CertificateLoader.LoadCertificateFromFile(crtPath);
            if (existing.NotAfter > DateTime.UtcNow.AddDays(7) && IsTrusted(existing, trustMarkerPath))
            {
                return (certDir, crtFileName, keyFileName);
            }
        }

        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest(Subject, rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

        var sanBuilder = new SubjectAlternativeNameBuilder();
        foreach (var name in _dnsNames)
        {
            sanBuilder.AddDnsName(name);
        }
        request.CertificateExtensions.Add(sanBuilder.Build());
        request.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, true));
        request.CertificateExtensions.Add(new X509KeyUsageExtension(
            X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment, true));
        request.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(
            [new Oid("1.3.6.1.5.5.7.3.1")], true));

        using var cert = request.CreateSelfSigned(
            DateTimeOffset.UtcNow.AddDays(-1),
            DateTimeOffset.UtcNow.AddYears(2));

        File.WriteAllText(crtPath, cert.ExportCertificatePem());
        File.WriteAllText(keyPath, rsa.ExportPkcs8PrivateKeyPem());

        TrustCertificate(cert, crtPath, trustMarkerPath);

        return (certDir, crtFileName, keyFileName);
    }

    private static bool IsTrusted(X509Certificate2 cert, string trustMarkerPath)
    {
        if (OperatingSystem.IsWindows())
        {
            using var store = new X509Store(StoreName.Root, StoreLocation.CurrentUser);
            store.Open(OpenFlags.ReadOnly);
            return store.Certificates.Find(X509FindType.FindByThumbprint, cert.Thumbprint, false).Count > 0;
        }

        if (!File.Exists(trustMarkerPath))
        {
            return false;
        }

        return File.ReadAllText(trustMarkerPath).Trim() == cert.Thumbprint;
    }

    private static void TrustCertificate(X509Certificate2 cert, string crtPath, string trustMarkerPath)
    {
        if (OperatingSystem.IsWindows())
        {
            using var store = new X509Store(StoreName.Root, StoreLocation.CurrentUser);
            store.Open(OpenFlags.ReadWrite);

            var stale = store.Certificates.Find(X509FindType.FindBySubjectName, "docspace.localhost", false);
            foreach (var old in stale)
            {
                store.Remove(old);
            }

            store.Add(cert);
            return;
        }

        if (OperatingSystem.IsMacOS())
        {
            Console.WriteLine("[AppHost] Installing dev certificate into the login keychain (password prompt may appear)...");

            var keychain = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Library", "Keychains", "login.keychain-db");

            var exitCode = RunProcess("security",
                ["add-trusted-cert", "-r", "trustRoot", "-k", keychain, crtPath]);

            if (exitCode == 0)
            {
                File.WriteAllText(trustMarkerPath, cert.Thumbprint);
            }
            else
            {
                Console.WriteLine($"[AppHost] 'security add-trusted-cert' exited with code {exitCode}. Trust the certificate manually: {crtPath}");
            }
            return;
        }

        if (OperatingSystem.IsLinux())
        {
            Console.WriteLine("[AppHost] Dev certificate generated. To trust it on Linux run (as root):");
            Console.WriteLine($"  sudo cp '{crtPath}' /usr/local/share/ca-certificates/docspace.localhost.crt && sudo update-ca-certificates");
            Console.WriteLine("  # For Chrome/Firefox (NSS): certutil -d sql:$HOME/.pki/nssdb -A -t \"C,,\" -n docspace.localhost -i '" + crtPath + "'");
            File.WriteAllText(trustMarkerPath, cert.Thumbprint);
        }
    }

    private static int RunProcess(string fileName, string[] args)
    {
        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            UseShellExecute = false,
            RedirectStandardOutput = false,
            RedirectStandardError = false
        };
        foreach (var a in args)
        {
            psi.ArgumentList.Add(a);
        }

        using var proc = Process.Start(psi);
        if (proc is null)
        {
            return -1;
        }
        proc.WaitForExit();
        return proc.ExitCode;
    }
}
