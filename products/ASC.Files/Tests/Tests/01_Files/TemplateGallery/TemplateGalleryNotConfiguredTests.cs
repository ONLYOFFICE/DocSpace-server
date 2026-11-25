// (c) Copyright Ascensio System SIA 2009-2026
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

namespace ASC.Files.Tests.Tests._01_Files.TemplateGallery;

/// <summary>
/// An installation may configure no template gallery (`files:template-gallery`). The check of uploaded PDFs and the
/// copy of a gallery form must then answer as for any other file, not fail on the missing section. Pure logic, no
/// portal.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "Forms")]
public class TemplateGalleryNotConfiguredTests
{
    private const string Signature = "ONLYOFFICEFORM";

    [Fact]
    public async Task CheckExtendedPDFstream_SignatureConfigured_RecognisesTheForm()
    {
        var fileChecker = new FileChecker(null!, Configuration([new("files:template-gallery:signature", Signature)]), null!, null!);

        (await fileChecker.CheckExtendedPDFstream(new MemoryStream(FormPdf()))).Should().BeTrue();
    }

    [Fact]
    public async Task CheckExtendedPDFstream_NoGallerySection_IsNotAForm()
    {
        var fileChecker = new FileChecker(null!, Configuration([]), null!, null!);

        (await fileChecker.CheckExtendedPDFstream(new MemoryStream(FormPdf()))).Should().BeFalse();
    }

    [Fact]
    public async Task Get_NoGallerySection_ReturnsNotFound()
    {
        var manager = new TemplateGalleryRequestManager(
            Microsoft.Extensions.Logging.Abstractions.NullLogger<TemplateGalleryRequestManager>.Instance, Configuration([]), null!);

        await Assert.ThrowsAsync<ItemNotFoundException>(() => manager.Get(1, ".pdf"));
    }

    private static IConfiguration Configuration(KeyValuePair<string, string?>[] values)
    {
        return new ConfigurationBuilder().AddInMemoryCollection(values).Build();
    }

    // The header of a PDF form as the editor writes it: the binary marker, the first object, the signature with its
    // metadata, and the stream that has to follow them.
    private static byte[] FormPdf()
    {
        return Encoding.Latin1.GetBytes($"%PDF-1.7\n%\xCD\xCA\xD2\xA9\u000D1 0 obj\x0A<<\x0A/{Signature} 1 2 3 >>\x0Astream\x0D\x0A");
    }
}
