using ASC.Webhooks.Core;

namespace ASC.Files.Core.Security;

[Scope]
public class WebhookFileEntryAccessChecker(
    AuthContext authContext,
    UserManager userManager,
    FileSecurity fileSecurity) : IWebhookAccessChecker<FileEntry>
{
    public async Task<bool> CheckAccessAsync(FileEntry fileEntry, Guid userId)
    {
        if (authContext.CurrentAccount.ID == userId) //TODO:  || userManager.IsSystemUser(authContext.CurrentAccount.ID) ?
        {
            return true;
        }

        var targetUserType = await userManager.GetUserTypeAsync(userId);

        if (targetUserType is EmployeeType.DocSpaceAdmin)
        {
            return true;
        }

        if (fileEntry is FileEntry<int> fileEntryInt)
        {
            return await fileSecurity.CanReadAsync(fileEntryInt, userId);
        }

        if (fileEntry is FileEntry<string> fileEntryString)
        {
            return await fileSecurity.CanReadAsync(fileEntryString, userId);
        }

        return false;
    }
}