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

namespace ASC.Files.Core;

/// <summary>
/// The data of the submitted forms.
/// </summary>
public class SubmitFormsData
{
    /// <summary>
    /// The list of forms data.
    /// </summary>
    [Nested]
    public IEnumerable<FormsItemData> FormsData { get; set; }
}

/// <summary>
/// The data of the separate form item.
/// </summary>
public class FormsItemData
{
    /// <summary>
    /// The form data key.
    /// </summary>
    /// <example>first_name</example>
    public string Key { get; set; }

    /// <summary>
    /// The form data tag.
    /// </summary>
    /// <example>personal_info</example>
    public string Tag { get; set; }

    /// <summary>
    /// The form data value.
    /// </summary>
    /// <example>John</example>
    public string Value { get; set; }

    /// <summary>
    /// The form data type.
    /// </summary>
    /// <example>text</example>
    public string Type { get; set; }
}

/// <summary>
/// The database of forms items data.
/// </summary>
[Transient]
public class DbFormsItemDataSearch : SubmitFormsData, ISearchItem
{
    /// <summary>
    /// The form ID.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// The tenant ID.
    /// </summary>
    public int TenantId { get; set; }

    /// <summary>
    /// The form parent ID.
    /// </summary>
    public int ParentId { get; set; }

    /// <summary>
    /// The original form ID.
    /// </summary>
    public int OriginalFormId { get; set; }

    /// <summary>
    /// The original form version.
    /// </summary>
    public int OriginalFormVersion { get; set; }

    /// <summary>
    /// The ID of the room where the form is located.
    /// </summary>
    public int RoomId { get; set; }

    /// <summary>
    /// The date and time when the form was created.
    /// </summary>
    public DateTime CreateOn { get; set; }

    /// <summary>
    /// The form index name.
    /// </summary>
    [Ignore]
    public string IndexName => "forms_data";

    public Expression<Func<ISearchItem, object[]>> GetSearchContentFields(SearchSettingsHelper searchSettings)
    {
        return a => new object[] { };
    }
}

/// <summary>
/// The OpenSearch document for form metadata (one instance per original form version).
/// </summary>
[Transient]
public class DbFormsMetadataSearch : ISearchItem
{
    /// <summary>
    /// The document ID. Encoded as a stable hash of (OriginalFormId, OriginalFormVersion).
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// The tenant ID.
    /// </summary>
    public int TenantId { get; set; }

    /// <summary>
    /// The original form ID.
    /// </summary>
    public int OriginalFormId { get; set; }

    /// <summary>
    /// The original form version.
    /// </summary>
    public int OriginalFormVersion { get; set; }

    /// <summary>
    /// The ID of the room where the form is located.
    /// </summary>
    public int RoomId { get; set; }

    /// <summary>
    /// The form field metadata.
    /// </summary>
    [Nested]
    public IEnumerable<FormMetadata> Metadata { get; set; }

    /// <summary>
    /// The metadata index name.
    /// </summary>
    [Ignore]
    public string IndexName => "forms_metadata";

    public Expression<Func<ISearchItem, object[]>> GetSearchContentFields(SearchSettingsHelper searchSettings)
        => _ => Array.Empty<object>();

    /// <summary>
    /// Computes a stable non-negative int document ID from the (OriginalFormId, OriginalFormVersion) pair.
    /// </summary>
    public static int ComputeId(int originalFormId, int originalFormVersion)
    {
        var hash = HashCode.Combine(originalFormId, originalFormVersion);
        return hash == int.MinValue ? int.MaxValue : Math.Abs(hash);
    }
}

[Scope]
public class BaseIndexerFormMetadata(Client client,
    ILogger<BaseIndexerFormMetadata> log,
    IDbContextFactory<WebstudioDbContext> dbContextManager,
    TenantManager tenantManager,
    BaseIndexerHelper baseIndexerHelper,
    Settings settings,
    IServiceProvider serviceProvider)
    : BaseIndexer<DbFormsMetadataSearch>(client, log, dbContextManager, tenantManager, baseIndexerHelper, settings, serviceProvider);

[Scope(typeof(IFactoryIndexer))]
public class FactoryIndexerFormMetadata(
    ILoggerProvider options,
    TenantManager tenantManager,
    SearchSettingsHelper searchSettingsHelper,
    FactoryIndexer factoryIndexer,
    BaseIndexerFormMetadata baseIndexer,
    IServiceProvider serviceProvider,
    ICache cache)
    : FactoryIndexer<DbFormsMetadataSearch>(options, tenantManager, searchSettingsHelper, factoryIndexer, baseIndexer, serviceProvider, cache)
{
    public override async Task IndexAllAsync()
    {
        try
        {
            var now = DateTime.UtcNow;

            await foreach (var _ in _indexer.IndexAllAsync(GetCount, GetIds, GetData))
            {

            }

            await _indexer.OnComplete(now);
        }
        catch (Exception e)
        {
            Logger.ErrorFactoryIndexerFile(e);
            throw;
        }

        return;

        List<int> GetIds(DateTime lastIndexed)
        {
            return [];
        }

        List<DbFormsMetadataSearch> GetData(long start, long stop, DateTime lastIndexed)
        {
            return [];

        }

        (int, int, int) GetCount(DateTime lastIndexed)
        {
            return new ValueTuple<int, int, int>(0, 0, 0);
        }
    }
}

[Scope]
public class BaseIndexerForm(Client client,
    ILogger<BaseIndexerForm> log,
    IDbContextFactory<WebstudioDbContext> dbContextManager,
    TenantManager tenantManager,
    BaseIndexerHelper baseIndexerHelper,
    Settings settings,
    IServiceProvider serviceProvider)
    : BaseIndexer<DbFormsItemDataSearch>(client, log, dbContextManager, tenantManager, baseIndexerHelper, settings, serviceProvider);

[Scope(typeof(IFactoryIndexer))]
public class FactoryIndexerForm(
    ILoggerProvider options,
    TenantManager tenantManager,
    SearchSettingsHelper searchSettingsHelper,
    FactoryIndexer factoryIndexer,
    BaseIndexerForm baseIndexer,
    IServiceProvider serviceProvider,
    ICache cache)
    : FactoryIndexer<DbFormsItemDataSearch>(options, tenantManager, searchSettingsHelper, factoryIndexer, baseIndexer, serviceProvider, cache)
{
    public override async Task IndexAllAsync()
    {
        try
        {
            var now = DateTime.UtcNow;

            await foreach (var _ in _indexer.IndexAllAsync(GetCount, GetIds, GetData))
            {

            }

            await _indexer.OnComplete(now);
        }
        catch (Exception e)
        {
            Logger.ErrorFactoryIndexerFile(e);
            throw;
        }

        return;

        List<int> GetIds(DateTime lastIndexed)
        {
            return [];
        }

        List<DbFormsItemDataSearch> GetData(long start, long stop, DateTime lastIndexed)
        {
            return [];

        }

        (int, int, int) GetCount(DateTime lastIndexed)
        {
            return new ValueTuple<int, int, int>(0, 0, 0);
        }
    }
}