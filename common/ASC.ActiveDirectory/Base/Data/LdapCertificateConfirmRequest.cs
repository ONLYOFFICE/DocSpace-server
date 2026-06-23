// Copyright (C) Ascensio System SIA, 2009-2026
// 
// This program is a free software product. You can redistribute it and/or
// modify it under the terms of the GNU Affero General Public License (AGPL)
// version 3 as published by the Free Software Foundation, together with the
// additional terms provided in the LICENSE file.
// 
// This program is distributed WITHOUT ANY WARRANTY, without even the implied
// warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. For
// details, see the GNU AGPL at: https://www.gnu.org/licenses/agpl-3.0.html
// 
// You can contact Ascensio System SIA by email at info@onlyoffice.com
// or by postal mail at 20A-6 Ernesta Birznieka-Upisha Street, Riga,
// LV-1050, Latvia, European Union.
// 
// The interactive user interfaces in modified versions of the Program
// are required to display Appropriate Legal Notices in accordance with
// Section 5 of the GNU AGPL version 3.
// 
// No trademark rights are granted under this License.
// 
// All non-code elements of the Product, including illustrations,
// icon sets, and technical writing content, are licensed under the
// Creative Commons Attribution-ShareAlike 4.0 International License:
// https://creativecommons.org/licenses/by-sa/4.0/legalcode
// 
// This license applies only to such non-code elements and does not
// modify or replace the licensing terms applicable to the Program's
// source code, which remains licensed under the GNU Affero General
// Public License v3.
// 
// SPDX-License-Identifier: AGPL-3.0-only

namespace ASC.ActiveDirectory.Base.Data;
public class LdapCertificateConfirmRequest
{
    private volatile bool _approved;
    private volatile bool _requested;
    private volatile string _serialNumber;
    private volatile string _issuerName;
    private volatile string _subjectName;
    private volatile string _hash;
    private volatile int[] _certificateErrors;

    public bool Approved { get => _approved;
        set => _approved = value;
    }

    public bool Requested { get => _requested;
        set => _requested = value;
    }

    public string SerialNumber { get => _serialNumber;
        set => _serialNumber = value;
    }

    public string IssuerName { get => _issuerName;
        set => _issuerName = value;
    }

    public string SubjectName { get => _subjectName;
        set => _subjectName = value;
    }

    public DateTime ValidFrom { get; set; }

    public DateTime ValidUntil { get; set; }

    public string Hash { get => _hash;
        set => _hash = value;
    }

    public int[] CertificateErrors { get => _certificateErrors;
        set => _certificateErrors = value;
    }

    private enum LdapCertificateProblem
    {
        CertExpired = -2146762495,
        CertCnNoMatch = -2146762481,
        // ReSharper disable once UnusedMember.Local
        CertIssuerChaining = -2146762489,
        CertUntrustedCa = -2146762478,
        // ReSharper disable once UnusedMember.Local
        CertUntrustedRoot = -2146762487,
        CertMalformed = -2146762488,
        CertUnrecognizedError = -2146762477
    }

    public static int[] GetLdapCertProblems(X509Certificate certificate, X509Chain chain,
        SslPolicyErrors sslPolicyErrors, ILogger log = null)
    {
        var certificateErrors = new List<int>();
        try
        {
            if (sslPolicyErrors == SslPolicyErrors.None)
            {
                return certificateErrors.ToArray();
            }

            var expDate = DateTime.Parse(certificate.GetExpirationDateString()).ToUniversalTime();
            var utcNow = DateTime.UtcNow;
            if (expDate < utcNow && expDate.AddDays(1) >= utcNow)
            {
                certificateErrors.Add((int)LdapCertificateProblem.CertExpired);
            }

            if (sslPolicyErrors.HasFlag(SslPolicyErrors.RemoteCertificateChainErrors))
            {
                certificateErrors.Add((int)LdapCertificateProblem.CertMalformed);
            }

            if (sslPolicyErrors.HasFlag(SslPolicyErrors.RemoteCertificateNameMismatch))
            {
                log?.WarnGetLdapCertProblems(Enum.GetName(typeof(SslPolicyErrors), LdapCertificateProblem.CertCnNoMatch));

                certificateErrors.Add((int)LdapCertificateProblem.CertCnNoMatch);
            }

            if (sslPolicyErrors.HasFlag(SslPolicyErrors.RemoteCertificateNotAvailable))
            {
                log?.WarnGetLdapCertProblems(Enum.GetName(typeof(SslPolicyErrors), LdapCertificateProblem.CertCnNoMatch));

                certificateErrors.Add((int)LdapCertificateProblem.CertUntrustedCa);
            }
        }
        catch (Exception ex)
        {
            log?.ErrorGetLdapCertProblems(ex);

            certificateErrors.Add((int)LdapCertificateProblem.CertUnrecognizedError);
        }

        return certificateErrors.ToArray();
    }

    public static LdapCertificateConfirmRequest FromCert(X509Certificate certificate, X509Chain chain,
        SslPolicyErrors sslPolicyErrors, bool approved = false, bool requested = false, ILogger log = null)
    {
        var certificateErrors = GetLdapCertProblems(certificate, chain, sslPolicyErrors, log);

        try
        {
            string serialNumber = "", issuerName = "", subjectName = "", hash = "";
            DateTime validFrom = DateTime.UtcNow, validUntil = DateTime.UtcNow;

            LdapUtils.SkipErrors(() => serialNumber = certificate.GetSerialNumberString(), log);
            LdapUtils.SkipErrors(() => issuerName = certificate.Issuer, log);
            LdapUtils.SkipErrors(() => subjectName = certificate.Subject, log);
            LdapUtils.SkipErrors(() => validFrom = DateTime.Parse(certificate.GetEffectiveDateString()), log);
            LdapUtils.SkipErrors(() => validUntil = DateTime.Parse(certificate.GetExpirationDateString()), log);
            LdapUtils.SkipErrors(() => hash = certificate.GetCertHashString(), log);

            var certificateConfirmRequest = new LdapCertificateConfirmRequest
            {
                SerialNumber = serialNumber,
                IssuerName = issuerName,
                SubjectName = subjectName,
                ValidFrom = validFrom,
                ValidUntil = validUntil,
                Hash = hash,
                CertificateErrors = certificateErrors,
                Approved = approved,
                Requested = requested
            };

            return certificateConfirmRequest;
        }
        catch (Exception ex)
        {
            log?.ErrorLdapCertificateConfirmRequest(ex);

            return null;
        }
    }

    public override string ToString()
    {
        return JsonSerializer.Serialize(this);
    }
}