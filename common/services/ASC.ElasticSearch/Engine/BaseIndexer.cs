// (c) Copyright Ascensio System SIA 2009-2024
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


namespace ASC.ElasticSearch;

public enum UpdateAction
{
    Add,
    Replace,
    Remove
}

[Singleton]
public class BaseIndexerHelper
{
    public ConcurrentDictionary<string, bool> IsExist { get; set; }

    private readonly ICacheNotify<ClearIndexAction> _notify;

    public BaseIndexerHelper(ICacheNotify<ClearIndexAction> cacheNotify)
    {
        IsExist = new ConcurrentDictionary<string, bool>();
        _notify = cacheNotify;
        _notify.Subscribe(a =>
        {
            IsExist.AddOrUpdate(a.Id, false, (_, _) => false);
        }, CacheNotifyAction.Any);
    }

    public async Task ClearAsync<T>(T t) where T : class, ISearchItem
    {
        await _notify.PublishAsync(new ClearIndexAction { Id = t.IndexName }, CacheNotifyAction.Any);
    }
}

public abstract class BaseIndexer<T>(Client client,
        ILogger<BaseIndexer<T>> logger,
        IDbContextFactory<WebstudioDbContext> dbContextFactory,
        TenantManager tenantManager,
        BaseIndexerHelper baseIndexerHelper,
        Settings settings,
        IServiceProvider serviceProvider)
    where T : class, ISearchItem
{
    public const int QueryLimit = 10000;

    protected internal T Wrapper => serviceProvider.GetService<T>();
    internal string IndexName => Wrapper.IndexName;

    private bool _isExist;
    private readonly ILogger _logger = logger;
    protected readonly TenantManager _tenantManager = tenantManager;
    private static readonly object _locker = new();

    public async IAsyncEnumerable<List<T>> IndexAllAsync(
        Func<DateTime, (int, int, int)> getCount,
        Func<DateTime, List<int>> getIds,
        Func<long, long, DateTime, List<T>> getData)
    {
        DateTime lastIndexed;

        await using (var webStudioDbContext = await dbContextFactory.CreateDbContextAsync())
        {
            lastIndexed = await Queries.LastIndexedAsync(webStudioDbContext, Wrapper.IndexName);
        }

        if (lastIndexed.Equals(DateTime.MinValue))
        {
            CreateIfNotExist(serviceProvider.GetService<T>());
        }

        var (count, max, min) = getCount(lastIndexed);
        _logger.DebugIndex(IndexName, count, max, min);

        var ids = new List<int> { min };
        ids.AddRange(getIds(lastIndexed));
        ids.Add(max);

        for (var i = 0; i < ids.Count - 1; i++)
        {
            yield return getData(ids[i], ids[i + 1], lastIndexed);
        }
    }

    public async Task OnComplete(DateTime lastModified)
    {
        await using (var webStudioDbContext = await dbContextFactory.CreateDbContextAsync())
        {
            await webStudioDbContext.AddOrUpdateAsync(q => q.WebstudioIndex, new DbWebstudioIndex
            {
            IndexName = Wrapper.IndexName,
                LastModified = lastModified
        });

            await webStudioDbContext.SaveChangesAsync();
        }

        _logger.DebugIndexCompleted(Wrapper.IndexName);
    }

    public async Task ReIndexAsync()
    {
        await ClearAsync();
    }

    public void CreateIfNotExist(T data)
    {
        try
        {
            if (CheckExist(data))
            {
                return;
            }

            lock (_locker)
            {
                IPromise<IAnalyzers> analyzers(AnalyzersDescriptor b)
                {
                    foreach (var c in AnalyzerExtensions.GetNames())
                    {
                        var c1 = c;
                        b.Custom(c1 + "custom", ca => ca.Tokenizer(c1).Filters(nameof(Filter.lowercase)).CharFilters(nameof(CharFilter.io)));
                    }

                    foreach (var c in CharFilterExtensions.GetNames())
                    {
                        if (c == nameof(CharFilter.io))
                        {
                            continue;
                        }

                        var charFilters = new List<string> { nameof(CharFilter.io), c };
                        b.Custom(c + "custom", ca => ca.Tokenizer(nameof(Analyzer.whitespace)).Filters(nameof(Filter.lowercase)).CharFilters(charFilters));
                    }

                    if (data is ISearchItemDocument)
                    {
                        b.Custom("document", ca => ca.Tokenizer(Analyzer.whitespace.ToStringFast()).Filters(nameof(Filter.lowercase)).CharFilters(nameof(CharFilter.io)));
                    }

                    return b;
                }

                client.Instance.Indices.Create(data.IndexName,
                    c =>
                    c.Map<T>(m => m.AutoMap())
                    .Settings(r => r.Analysis(a =>
                                    a.Analyzers(analyzers)
                                    .CharFilters(d => d.HtmlStrip(CharFilter.html.ToStringFast())
                                    .Mapping(CharFilter.io.ToStringFast(), m => m.Mappings("ё => е", "Ё => Е"))))));

                _isExist = true;
            }
        }
        catch (Exception e)
        {
            _logger.ErrorCreateIfNotExist(e);
        }
    }

    public void Flush()
    {
        client.Instance.Indices.Flush(new FlushRequest(IndexName));
    }

    public void Refresh()
    {
        client.Instance.Indices.Refresh(new RefreshRequest(IndexName));
    }

    internal async Task IndexAsync(T data, bool immediately = true)
    {
        if (!(await BeforeIndexAsync(data)))
        {
            return;
        }

        await client.Instance.IndexAsync(data, idx => GetMeta(idx, data, immediately));
    }

    internal async Task IndexAsync(List<T> data, bool immediately = true)
    {
        if (data.Count == 0)
        {
            return;
        }

        if (!CheckExist(data[0]))
        {
            return;
        }

        if (data[0] is ISearchItemDocument)
        {
            var currentLength = 0L;
            var portion = new List<T>();
            var portionStart = 0;

            for (var i = 0; i < data.Count; i++)
            {
                var t = data[i];
                var runBulk = i == data.Count - 1;

                await BeforeIndexAsync(t);

                if (t is not ISearchItemDocument wwd || wwd.Document == null || string.IsNullOrEmpty(wwd.Document.Data))
                {
                    portion.Add(t);
                }
                else
                {
                    var dLength = wwd.Document.Data.Length;
                    if (dLength >= settings.MaxContentLength)
                    {
                        try
                        {
                            await IndexAsync(t, immediately);
                        }
                        catch (OpenSearchClientException e)
                        {
                            if (e.Response.HttpStatusCode == 429)
                            {
                                throw;
                            }
                            
                            _logger.ErrorIndex(e);
                        }
                        catch (Exception e)
                        {
                            _logger.ErrorIndex(e);
                        }
                        finally
                        {
                            wwd.Document.Data = null;
                            wwd.Document = null;
                            GC.Collect();
                        }

                        continue;
                    }

                    if (currentLength + dLength < settings.MaxContentLength)
                    {
                        portion.Add(t);
                        currentLength += dLength;
                    }
                    else
                    {
                        runBulk = true;
                        i--;
                    }
                }

                if (runBulk)
                {
                    var portion1 = portion.ToList();
                    await client.Instance.BulkAsync(r => r.IndexMany(portion1, GetMeta).SourceExcludes("attachments"));
                    for (var j = portionStart; j < i; j++)
                    {
                        if (data[j] is ISearchItemDocument { Document: not null } doc)
                        {
                            doc.Document.Data = null;
                            doc.Document = null;
                        }
                    }

                    portionStart = i;
                    portion = [];
                    currentLength = 0L;
                    GC.Collect();
                }
            }
        }
        else
        {
            foreach (var item in data)
            {
                await BeforeIndexAsync(item);
            }

            await client.Instance.BulkAsync(r => r.IndexMany(data, GetMeta));
        }
    }

    internal void Update(T data, bool immediately = true, params Expression<Func<T, object>>[] fields)
    {
        if (!CheckExist(data))
        {
            return;
        }

        client.Instance.Update(DocumentPath<T>.Id(data), r => GetMetaForUpdate(r, data, immediately, fields));
    }

    internal void Update(T data, UpdateAction action, Expression<Func<T, IList>> fields, bool immediately = true)
    {
        if (!CheckExist(data))
        {
            return;
        }

        client.Instance.Update(DocumentPath<T>.Id(data), r => GetMetaForUpdate(r, data, action, fields, immediately));
    }

    internal void Update(T data, Expression<Func<Selector<T>, Selector<T>>> expression, int tenantId, bool immediately = true, params Expression<Func<T, object>>[] fields)
    {
        if (!CheckExist(data))
        {
            return;
        }

        client.Instance.UpdateByQuery(GetDescriptorForUpdate(data, expression, tenantId, immediately, fields));
    }

    internal void Update(T data, Expression<Func<Selector<T>, Selector<T>>> expression, int tenantId, UpdateAction action, Expression<Func<T, IList>> fields, bool immediately = true)
    {
        if (!CheckExist(data))
        {
            return;
        }

        client.Instance.UpdateByQuery(GetDescriptorForUpdate(data, expression, tenantId, action, fields, immediately));
    }

    internal void Delete(T data, bool immediately = true)
    {
        client.Instance.Delete<T>(data, r => GetMetaForDelete(r, immediately));
    }

    internal void Delete(Expression<Func<Selector<T>, Selector<T>>> expression, int tenantId, bool immediately = true)
    {
        client.Instance.DeleteByQuery(GetDescriptorForDelete(expression, tenantId, immediately));
    }

    internal bool CheckExist(T data)
    {
        try
        {
            var isExist = baseIndexerHelper.IsExist.GetOrAdd(data.IndexName, k => client.Instance.Indices.Exists(k).Exists);
            if (isExist)
            {
                return true;
            }

            lock (_locker)
            {
                isExist = client.Instance.Indices.Exists(data.IndexName).Exists;

                baseIndexerHelper.IsExist.TryUpdate(data.IndexName, _isExist, false);

                if (isExist)
                {
                    return true;
                }
            }
        }
        catch (Exception e)
        {
            _logger.ErrorCheckExist(data.IndexName, e);
        }

        return false;
    }

    internal async Task<IReadOnlyCollection<T>> SelectAsync(Expression<Func<Selector<T>, Selector<T>>> expression, bool onlyId = false)
    {
        var func = expression.Compile();
        var selector = new Selector<T>(serviceProvider);
        var tenant = _tenantManager.GetCurrentTenant();
        var descriptor = func(selector).Where(r => r.TenantId, tenant.Id);

        return (await client.Instance.SearchAsync(descriptor.GetDescriptor(this, onlyId))).Documents;
    }

    internal (IReadOnlyCollection<T>, long) SelectWithTotal(Expression<Func<Selector<T>, Selector<T>>> expression, bool onlyId)
    {
        var func = expression.Compile();
        var selector = new Selector<T>(serviceProvider);
        var tenant = _tenantManager.GetCurrentTenant();
        var descriptor = func(selector).Where(r => r.TenantId, tenant.Id);
        var result = client.Instance.Search(descriptor.GetDescriptor(this, onlyId));
        var total = result.Total;

        return (result.Documents, total);
    }

    protected virtual Task<bool> BeforeIndexAsync(T data)
    {
        return Task.FromResult(CheckExist(data));
    }

    private async Task ClearAsync()
    {
        await using var webstudioDbContext = await dbContextFactory.CreateDbContextAsync();
        var index = await Queries.IndexAsync(webstudioDbContext, Wrapper.IndexName);

        if (index != null)
        {
            webstudioDbContext.WebstudioIndex.Remove(index);
            await webstudioDbContext.SaveChangesAsync();
        }

        _logger.DebugIndexDeleted(Wrapper.IndexName);
        await client.Instance.Indices.DeleteAsync(Wrapper.IndexName);
        await baseIndexerHelper.ClearAsync(Wrapper);
        CreateIfNotExist(Wrapper);
    }

    private IIndexRequest<T> GetMeta(IndexDescriptor<T> request, T data, bool immediately = true)
    {
        var result = request.Index(data.IndexName).Id(data.Id);

        if (immediately)
        {
            result.Refresh(OpenSearch.Net.Refresh.True);
        }

        if (data is ISearchItemDocument)
        {
            result.Pipeline("attachments");
        }

        return result;
    }
    private IBulkIndexOperation<T> GetMeta(BulkIndexDescriptor<T> desc, T data)
    {
        var result = desc.Index(IndexName).Id(data.Id);

        if (data is ISearchItemDocument { Document: not null })
        {
            result.Pipeline("attachments");
        }

        return result;
    }

    private IUpdateRequest<T, T> GetMetaForUpdate(UpdateDescriptor<T, T> request, T data, bool immediately = true, params Expression<Func<T, object>>[] fields)
    {
        var result = request.Index(IndexName);

        if (fields.Length > 0)
        {
            result.Script(GetScriptUpdateByQuery(data, fields));
        }
        else
        {
            result.Doc(data);
        }

        if (immediately)
        {
            result.Refresh(OpenSearch.Net.Refresh.True);
        }

        return result;
    }

    private Func<ScriptDescriptor, IScript> GetScriptUpdateByQuery(T data, params Expression<Func<T, object>>[] fields)
    {
        var source = new StringBuilder();
        var parameters = new Dictionary<string, object>();

        for (var i = 0; i < fields.Length; i++)
        {
            var field = fields[i];
            var func = field.Compile();
            var newValue = func(data);
            string name;

            var expression = field.Body;
            var isList = expression.Type.IsGenericType && expression.Type.GetGenericTypeDefinition() == typeof(List<>);


            var sourceExprText = "";

            while (!string.IsNullOrEmpty(name = TryGetName(expression, out var member)))
            {
                sourceExprText = "." + name + sourceExprText;
                expression = member.Expression;
            }

            if (isList)
            {
                UpdateByAction(UpdateAction.Add, (IList)newValue, sourceExprText, parameters, source);
            }
            else
            {
                if (newValue == default(T))
                {
                    source.Append($"ctx._source.remove('{sourceExprText[1..]}');");
                }
                else
                {
                    var pkey = "p" + sourceExprText.Replace(".", "");
                    source.Append($"ctx._source{sourceExprText} = params.{pkey};");
                    parameters.Add(pkey, newValue);
                }
            }
        }

        var sourceData = source.ToString();

        return r => r.Source(sourceData).Params(parameters);
    }

    private IUpdateRequest<T, T> GetMetaForUpdate(UpdateDescriptor<T, T> request, T data, UpdateAction action, Expression<Func<T, IList>> fields, bool immediately = true)
    {
        var result = request.Index(IndexName).Script(GetScriptForUpdate(data, action, fields));

        if (immediately)
        {
            result.Refresh(OpenSearch.Net.Refresh.True);
        }

        return result;
    }

    private Func<ScriptDescriptor, IScript> GetScriptForUpdate(T data, UpdateAction action, Expression<Func<T, IList>> fields)
    {
        var source = new StringBuilder();

        var func = fields.Compile();
        var newValue = func(data);
        string name;

        var expression = fields.Body;

        var sourceExprText = "";

        while (!string.IsNullOrEmpty(name = TryGetName(expression, out var member)))
        {
            sourceExprText = "." + name + sourceExprText;
            expression = member.Expression;
        }

        var parameters = new Dictionary<string, object>();

        UpdateByAction(action, newValue, sourceExprText, parameters, source);

        return r => r.Source(source.ToString()).Params(parameters);
    }

    private void UpdateByAction(UpdateAction action, IList newValue, string key, Dictionary<string, object> parameters, StringBuilder source)
    {
        var paramKey = "p" + key.Replace(".", "");
        switch (action)
        {
            case UpdateAction.Add:
                for (var i = 0; i < newValue.Count; i++)
                {
                    parameters.Add(paramKey + i, newValue[i]);
                    source.Append($"if (!ctx._source{key}.contains(params.{paramKey + i})){{ctx._source{key}.add(params.{paramKey + i})}}");
                }
                break;
            case UpdateAction.Replace:
                parameters.Add(paramKey, newValue);
                source.Append($"ctx._source{key} = params.{paramKey};");
                break;
            case UpdateAction.Remove:
                for (var i = 0; i < newValue.Count; i++)
                {
                    parameters.Add(paramKey + i, newValue[i]);
                    source.Append($"ctx._source{key}.removeIf(item -> item.id == params.{paramKey + i}.id)");
                }
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(action), action, null);
        }
    }

    private string TryGetName(Expression expr, out MemberExpression member)
    {
        member = expr as MemberExpression;
        if (member == null && expr is UnaryExpression unary)
        {
                member = unary.Operand as MemberExpression;
        }

        return member == null ? "" : member.Member.Name.ToLowerCamelCase();
    }

    private IDeleteRequest GetMetaForDelete(DeleteDescriptor<T> request, bool immediately = true)
    {
        var result = request.Index(IndexName);
        if (immediately)
        {
            result.Refresh(OpenSearch.Net.Refresh.True);
        }

        return result;
    }

    private Func<DeleteByQueryDescriptor<T>, IDeleteByQueryRequest> GetDescriptorForDelete(Expression<Func<Selector<T>, Selector<T>>> expression, int tenantId, bool immediately = true)
    {
        var func = expression.Compile();
        var selector = new Selector<T>(serviceProvider);
        var descriptor = func(selector).Where(r => r.TenantId, tenantId);

        return descriptor.GetDescriptorForDelete(this, immediately);
    }

    private Func<UpdateByQueryDescriptor<T>, IUpdateByQueryRequest> GetDescriptorForUpdate(T data, Expression<Func<Selector<T>, Selector<T>>> expression, int tenantId, bool immediately = true, params Expression<Func<T, object>>[] fields)
    {
        var func = expression.Compile();
        var selector = new Selector<T>(serviceProvider);
        var descriptor = func(selector).Where(r => r.TenantId, tenantId);

        return descriptor.GetDescriptorForUpdate(this, GetScriptUpdateByQuery(data, fields), immediately);
    }

    private Func<UpdateByQueryDescriptor<T>, IUpdateByQueryRequest> GetDescriptorForUpdate(T data, Expression<Func<Selector<T>, Selector<T>>> expression, int tenantId, UpdateAction action, Expression<Func<T, IList>> fields, bool immediately = true)
    {
        var func = expression.Compile();
        var selector = new Selector<T>(serviceProvider);
        var descriptor = func(selector).Where(r => r.TenantId, tenantId);

        return descriptor.GetDescriptorForUpdate(this, GetScriptForUpdate(data, action, fields), immediately);
    }
}

static class CamelCaseExtension
{
    internal static string ToLowerCamelCase(this string str)
    {
        return str.ToLowerInvariant()[0] + str[1..];
    }
}


static file class Queries
{
    public static readonly Func<WebstudioDbContext, string, Task<DateTime>> LastIndexedAsync =
        EF.CompileAsyncQuery(
            (WebstudioDbContext ctx, string indexName) =>
                ctx.WebstudioIndex
                    .Where(r => r.IndexName == indexName)
                    .Select(r => r.LastModified)
                    .FirstOrDefault());

    public static readonly Func<WebstudioDbContext, string, Task<DbWebstudioIndex>> IndexAsync =
        EF.CompileAsyncQuery(
            (WebstudioDbContext ctx, string indexName) =>
                ctx.WebstudioIndex
                    .FirstOrDefault(r => r.IndexName == indexName));
}