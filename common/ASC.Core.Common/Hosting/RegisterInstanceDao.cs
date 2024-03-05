// (c) Copyright Ascensio System SIA 2010-2023
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

namespace ASC.Core.Common.Hosting;

[Scope]
public class RegisterInstanceDao<T>(
    ILogger<RegisterInstanceDao<T>> logger,
    IDbContextFactory<InstanceRegistrationContext> dbContextFactory) : IRegisterInstanceDao<T> where T : IHostedService
{
    private readonly InstanceRegistrationContext _instanceRegistrationContext = dbContextFactory.CreateDbContext();
    
    public async Task AddOrUpdateAsync(InstanceRegistration obj)
    {
        var inst = await _instanceRegistrationContext.InstanceRegistrations.FindAsync(obj.InstanceRegistrationId);

        if (inst == null)
        {
            await _instanceRegistrationContext.AddAsync(obj);
        }
        else
        {
            _instanceRegistrationContext.Entry(inst).CurrentValues.SetValues(obj);
            _instanceRegistrationContext.Entry(inst).State = EntityState.Modified;
        }

        bool saveFailed;

        do
        {
            saveFailed = false; 

            try
            {
                await _instanceRegistrationContext.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException ex)
            {
                logger.TraceDbUpdateConcurrencyException(obj.InstanceRegistrationId, DateTimeOffset.Now);

                saveFailed = true;

                var entry = ex.Entries.Single();

                if (entry.State == EntityState.Modified)
                {
                    entry.State = EntityState.Added;
                }
            }
        }
        while (saveFailed);
    }

    public async Task<List<InstanceRegistration>> GetAllAsync(string workerTypeName)
    {
        return await Queries.InstanceRegistrationsAsync(_instanceRegistrationContext, workerTypeName).ToListAsync();
    }

    public async Task DeleteAsync(string instanceId)
    {
        var item = await _instanceRegistrationContext.InstanceRegistrations.FindAsync(instanceId);

        if (item == null)
        {
            return;
        }

        _instanceRegistrationContext.InstanceRegistrations.Remove(item);

        try
        {
            await _instanceRegistrationContext.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException ex)
        {
            var entry = ex.Entries.Single();

            if (entry.State == EntityState.Deleted)
            {
                entry.State = EntityState.Detached;
            }
        }
    }

}

 static file class Queries
 {
     public static readonly Func<InstanceRegistrationContext, string, IAsyncEnumerable<InstanceRegistration>>
         InstanceRegistrationsAsync = Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
             (InstanceRegistrationContext ctx, string workerTypeName) =>
                 ctx.InstanceRegistrations.Where(x => x.WorkerTypeName == workerTypeName));
 }