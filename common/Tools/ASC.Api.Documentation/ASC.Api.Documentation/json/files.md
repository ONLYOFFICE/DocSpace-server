# Api

All URIs are relative to *http://localhost:8092*

## Endpoints

| Class | Method | HTTP request | Description |
|------------ | ------------- | ------------- | -------------|
| *FilesFilesApi* | [**addFileToRecent**](#addfiletorecent) | **POST** /api/2.0/files/file/{fileId}/recent | Add a file to the Recent section |
| *FilesFilesApi* | [**addTemplates**](#addtemplates) | **POST** /api/2.0/files/templates | Add template files |
| *FilesFilesApi* | [**changeVersionHistory**](#changeversionhistory) | **PUT** /api/2.0/files/file/{fileId}/history | Change version history |
| *FilesFilesApi* | [**checkFillFormDraft**](#checkfillformdraft) | **POST** /api/2.0/files/masterform/{fileId}/checkfillformdraft | Check the form draft filling |
| *FilesFilesApi* | [**copyFileAs**](#copyfileas) | **POST** /api/2.0/files/file/{fileId}/copyas | Copy a file |
| *FilesFilesApi* | [**createEditSession**](#createeditsession) | **POST** /api/2.0/files/file/{fileId}/edit_session | Create the editing session |
| *FilesFilesApi* | [**createFile**](#createfile) | **POST** /api/2.0/files/{folderId}/file | Create a file |
| *FilesFilesApi* | [**createFileInMyDocuments**](#createfileinmydocuments) | **POST** /api/2.0/files/@my/file | Create a file in the My documents section |
| *FilesFilesApi* | [**createFilePrimaryExternalLink**](#createfileprimaryexternallink) | **POST** /api/2.0/files/file/{id}/link | Create primary external link |
| *FilesFilesApi* | [**createHtmlFile**](#createhtmlfile) | **POST** /api/2.0/files/{folderId}/html | Create an HTML file |
| *FilesFilesApi* | [**createHtmlFileInMyDocuments**](#createhtmlfileinmydocuments) | **POST** /api/2.0/files/@my/html | Create an HTML file in the My documents section |
| *FilesFilesApi* | [**createTextFile**](#createtextfile) | **POST** /api/2.0/files/{folderId}/text | Create a text file |
| *FilesFilesApi* | [**createTextFileInMyDocuments**](#createtextfileinmydocuments) | **POST** /api/2.0/files/@my/text | Create a text file in the My documents section |
| *FilesFilesApi* | [**createThumbnails**](#createthumbnails) | **POST** /api/2.0/files/thumbnails | Create file thumbnails |
| *FilesFilesApi* | [**deleteFile**](#deletefile) | **DELETE** /api/2.0/files/file/{fileId} | Delete a file |
| *FilesFilesApi* | [**deleteRecent**](#deleterecent) | **DELETE** /api/2.0/files/recent | Delete recent files |
| *FilesFilesApi* | [**deleteTemplates**](#deletetemplates) | **DELETE** /api/2.0/files/templates | Delete template files |
| *FilesFilesApi* | [**generateXlsx**](#generatexlsx) | **POST** /api/2.0/files/file/{fileId}/xlsx | Generate XLSX report |
| *FilesFilesApi* | [**getAllFormRoles**](#getallformroles) | **GET** /api/2.0/files/file/{fileId}/formroles | Get form roles |
| *FilesFilesApi* | [**getEditDiffUrl**](#geteditdiffurl) | **GET** /api/2.0/files/file/{fileId}/edit/diff | Get changes URL |
| *FilesFilesApi* | [**getEditHistory**](#getedithistory) | **GET** /api/2.0/files/file/{fileId}/edit/history | Get version history |
| *FilesFilesApi* | [**getEncryptionInfo**](#getencryptioninfo) | **GET** /api/2.0/files/{fileId}/access | Get file encryption information |
| *FilesFilesApi* | [**getFileHistory**](#getfilehistory) | **GET** /api/2.0/files/file/{fileId}/log | Get file history |
| *FilesFilesApi* | [**getFileInfo**](#getfileinfo) | **GET** /api/2.0/files/file/{fileId} | Get file information |
| *FilesFilesApi* | [**getFileLinks**](#getfilelinks) | **GET** /api/2.0/files/file/{id}/links | Get file external links |
| *FilesFilesApi* | [**getFilePrimaryExternalLink**](#getfileprimaryexternallink) | **GET** /api/2.0/files/file/{id}/link | Get primary external link |
| *FilesFilesApi* | [**getFileVersionInfo**](#getfileversioninfo) | **GET** /api/2.0/files/file/{fileId}/history | Get file versions |
| *FilesFilesApi* | [**getFillResult**](#getfillresult) | **GET** /api/2.0/files/file/fillresult | Get form-filling result |
| *FilesFilesApi* | [**getFormSubmissions**](#getformsubmissions) | **GET** /api/2.0/files/file/{fileId}/submissions | Get form submission results |
| *FilesFilesApi* | [**getPresignedFileUri**](#getpresignedfileuri) | **GET** /api/2.0/files/file/{fileId}/presigned | Get file download link asynchronously |
| *FilesFilesApi* | [**getPresignedUri**](#getpresigneduri) | **GET** /api/2.0/files/file/{fileId}/presigneduri | Get file download link |
| *FilesFilesApi* | [**getProtectedFileUsers**](#getprotectedfileusers) | **GET** /api/2.0/files/file/{fileId}/protectusers | Get users access rights to the protected file |
| *FilesFilesApi* | [**getReferenceData**](#getreferencedata) | **POST** /api/2.0/files/file/referencedata | Get reference data |
| *FilesFilesApi* | [**getXlsx**](#getxlsx) | **GET** /api/2.0/files/file/{fileId}/xlsx | Get XLSX report generation status |
| *FilesFilesApi* | [**isFormPDF**](#isformpdf) | **GET** /api/2.0/files/file/{fileId}/isformpdf | Check the PDF file |
| *FilesFilesApi* | [**lockFile**](#lockfile) | **PUT** /api/2.0/files/file/{fileId}/lock | Lock a file |
| *FilesFilesApi* | [**manageFormFilling**](#manageformfilling) | **PUT** /api/2.0/files/file/{fileId}/manageformfilling | Perform form filling action |
| *FilesFilesApi* | [**openEditFile**](#openeditfile) | **GET** /api/2.0/files/file/{fileId}/openedit | Open a file configuration |
| *FilesFilesApi* | [**restoreFileVersion**](#restorefileversion) | **POST** /api/2.0/files/file/{fileId}/restoreversion | Restore a file version |
| *FilesFilesApi* | [**saveEditingFileFromForm**](#saveeditingfilefromform) | **PUT** /api/2.0/files/file/{fileId}/saveediting | Save file edits |
| *FilesFilesApi* | [**saveFileAsPdf**](#savefileaspdf) | **POST** /api/2.0/files/file/{id}/saveaspdf | Save a file as PDF |
| *FilesFilesApi* | [**saveFormRoleMapping**](#saveformrolemapping) | **POST** /api/2.0/files/file/{fileId}/formrolemapping | Save form role mapping |
| *FilesFilesApi* | [**setCustomFilterTag**](#setcustomfiltertag) | **PUT** /api/2.0/files/file/{fileId}/customfilter | Set the Custom Filter editing mode |
| *FilesFilesApi* | [**setEncryptionInfo**](#setencryptioninfo) | **PUT** /api/2.0/files/{fileId}/access | Set file encryption information |
| *FilesFilesApi* | [**setFileExternalLink**](#setfileexternallink) | **PUT** /api/2.0/files/file/{id}/links | Set an external link |
| *FilesFilesApi* | [**setFileOrder**](#setfileorder) | **PUT** /api/2.0/files/{fileId}/order | Set file order |
| *FilesFilesApi* | [**setFilesOrder**](#setfilesorder) | **PUT** /api/2.0/files/order | Set order of files |
| *FilesFilesApi* | [**startEditFile**](#starteditfile) | **POST** /api/2.0/files/file/{fileId}/startedit | Start file editing |
| *FilesFilesApi* | [**startFillingFile**](#startfillingfile) | **PUT** /api/2.0/files/file/{fileId}/startfilling | Start file filling |
| *FilesFilesApi* | [**toggleFileFavorite**](#togglefilefavorite) | **GET** /api/2.0/files/favorites/{fileId} | Change the file favorite status |
| *FilesFilesApi* | [**trackEditFile**](#trackeditfile) | **GET** /api/2.0/files/file/{fileId}/trackeditfile | Track file editing |
| *FilesFilesApi* | [**updateFile**](#updatefile) | **PUT** /api/2.0/files/file/{fileId} | Update a file |
| *FilesFoldersApi* | [**checkUpload**](#checkupload) | **POST** /api/2.0/files/{folderId}/upload/check | Check file uploads |
| *FilesFoldersApi* | [**createFolder**](#createfolder) | **POST** /api/2.0/files/folder/{folderId} | Create a folder |
| *FilesFoldersApi* | [**createFolderPrimaryExternalLink**](#createfolderprimaryexternallink) | **POST** /api/2.0/files/folder/{id}/link | Create primary external link |
| *FilesFoldersApi* | [**createReportFolderHistory**](#createreportfolderhistory) | **POST** /api/2.0/files/folder/{folderId}/log/report | Start the folder history report generation |
| *FilesFoldersApi* | [**deleteFolder**](#deletefolder) | **DELETE** /api/2.0/files/folder/{folderId} | Delete a folder |
| *FilesFoldersApi* | [**generateXlsxByFolder**](#generatexlsxbyfolder) | **POST** /api/2.0/files/folder/{folderId}/xlsx | Generate XLSX report by folder |
| *FilesFoldersApi* | [**getFavoritesFolder**](#getfavoritesfolder) | **GET** /api/2.0/files/@favorites | Get the Favorites section |
| *FilesFoldersApi* | [**getFilesUsedSpace**](#getfilesusedspace) | **GET** /api/2.0/files/filesusedspace | Get used space of files |
| *FilesFoldersApi* | [**getFolder**](#getfolder) | **GET** /api/2.0/files/{folderId}/formfilter | Get folder form filter |
| *FilesFoldersApi* | [**getFolderByFolderId**](#getfolderbyfolderid) | **GET** /api/2.0/files/{folderId} | Get a folder by ID |
| *FilesFoldersApi* | [**getFolderHistory**](#getfolderhistory) | **GET** /api/2.0/files/folder/{folderId}/log | Get folder history |
| *FilesFoldersApi* | [**getFolderInfo**](#getfolderinfo) | **GET** /api/2.0/files/folder/{folderId} | Get folder information |
| *FilesFoldersApi* | [**getFolderLinks**](#getfolderlinks) | **GET** /api/2.0/files/folder/{id}/links | Get the folder links |
| *FilesFoldersApi* | [**getFolderPath**](#getfolderpath) | **GET** /api/2.0/files/folder/{folderId}/path | Get the folder path |
| *FilesFoldersApi* | [**getFolderPrimaryExternalLink**](#getfolderprimaryexternallink) | **GET** /api/2.0/files/folder/{id}/link | Get primary external link |
| *FilesFoldersApi* | [**getFolders**](#getfolders) | **GET** /api/2.0/files/{folderId}/subfolders | Get subfolders |
| *FilesFoldersApi* | [**getFormsFolder**](#getformsfolder) | **GET** /api/2.0/files/@forms | Get the Forms section |
| *FilesFoldersApi* | [**getMyFolder**](#getmyfolder) | **GET** /api/2.0/files/@my | Get the My documents section |
| *FilesFoldersApi* | [**getNewFolderItems**](#getnewfolderitems) | **GET** /api/2.0/files/{folderId}/news | Get new folder items |
| *FilesFoldersApi* | [**getRecentFolder**](#getrecentfolder) | **GET** /api/2.0/files/recent | Get the Recent section |
| *FilesFoldersApi* | [**getReportFolderHistory**](#getreportfolderhistory) | **GET** /api/2.0/files/folder/{folderId}/log/report | Get the folder history report generation status |
| *FilesFoldersApi* | [**getRootFolders**](#getrootfolders) | **GET** /api/2.0/files/@root | Get filtered sections |
| *FilesFoldersApi* | [**getTrashFolder**](#gettrashfolder) | **GET** /api/2.0/files/@trash | Get the Trash section |
| *FilesFoldersApi* | [**insertFile**](#insertfile) | **POST** /api/2.0/files/{folderId}/insert | Insert a file |
| *FilesFoldersApi* | [**insertFileToMyFromBody**](#insertfiletomyfrombody) | **POST** /api/2.0/files/@my/insert | Insert a file to the My documents section |
| *FilesFoldersApi* | [**renameFolder**](#renamefolder) | **PUT** /api/2.0/files/folder/{folderId} | Rename a folder |
| *FilesFoldersApi* | [**setFolderOrder**](#setfolderorder) | **PUT** /api/2.0/files/folder/{folderId}/order | Set folder order |
| *FilesFoldersApi* | [**setFolderPrimaryExternalLink**](#setfolderprimaryexternallink) | **PUT** /api/2.0/files/folder/{id}/links | Set the folder external link |
| *FilesFoldersApi* | [**terminateReportFolderHistory**](#terminatereportfolderhistory) | **DELETE** /api/2.0/files/folder/{folderId}/log/report | Terminate the folder history report generation |
| *FilesFoldersApi* | [**uploadFile**](#uploadfile) | **POST** /api/2.0/files/{folderId}/upload | Upload a file |
| *FilesFoldersApi* | [**uploadFileToMy**](#uploadfiletomy) | **POST** /api/2.0/files/@my/upload | Upload a file to the My documents section |
| *FilesOperationsApi* | [**abortUploadSession**](#abortuploadsession) | **DELETE** /api/2.0/files/{folderId}/session/{sessionId} | Aborts an in-progress file upload session. |
| *FilesOperationsApi* | [**addFavorites**](#addfavorites) | **POST** /api/2.0/files/favorites | Add favorite files and folders |
| *FilesOperationsApi* | [**bulkDownload**](#bulkdownload) | **PUT** /api/2.0/files/fileops/bulkdownload | Bulk download |
| *FilesOperationsApi* | [**checkConversionStatus**](#checkconversionstatus) | **GET** /api/2.0/files/file/{fileId}/checkconversion | Get conversion status |
| *FilesOperationsApi* | [**checkMoveOrCopyBatchItems**](#checkmoveorcopybatchitems) | **GET** /api/2.0/files/fileops/move | Move or copy files to a folder |
| *FilesOperationsApi* | [**checkMoveOrCopyDestFolder**](#checkmoveorcopydestfolder) | **GET** /api/2.0/files/fileops/checkdestfolder | Check for moving or copying files to a folder |
| *FilesOperationsApi* | [**copyBatchItems**](#copybatchitems) | **PUT** /api/2.0/files/fileops/copy | Copy to the folder |
| *FilesOperationsApi* | [**createUploadSession**](#createuploadsession) | **POST** /api/2.0/files/{folderId}/upload/create_session | Chunked upload |
| *FilesOperationsApi* | [**createUploadSessionInFolder**](#createuploadsessioninfolder) | **POST** /api/2.0/files/{folderId}/session | Creates a session for uploading a file to a specific folder in chunks. |
| *FilesOperationsApi* | [**deleteBatchItems**](#deletebatchitems) | **PUT** /api/2.0/files/fileops/delete | Delete files and folders |
| *FilesOperationsApi* | [**deleteFavoritesFromBody**](#deletefavoritesfrombody) | **DELETE** /api/2.0/files/favorites | Delete favorite files and folders (using body parameters) |
| *FilesOperationsApi* | [**deleteFileVersions**](#deletefileversions) | **PUT** /api/2.0/files/fileops/deleteversion | Delete file versions |
| *FilesOperationsApi* | [**duplicateBatchItems**](#duplicatebatchitems) | **PUT** /api/2.0/files/fileops/duplicate | Duplicate files and folders |
| *FilesOperationsApi* | [**emptyTrash**](#emptytrash) | **PUT** /api/2.0/files/fileops/emptytrash | Empty the Trash folder |
| *FilesOperationsApi* | [**finalizeSession**](#finalizesession) | **PUT** /api/2.0/files/{folderId}/session/{sessionId}/finalize | Finalize an upload session |
| *FilesOperationsApi* | [**getOperationStatuses**](#getoperationstatuses) | **GET** /api/2.0/files/fileops | Get active file operations |
| *FilesOperationsApi* | [**getOperationStatusesByType**](#getoperationstatusesbytype) | **GET** /api/2.0/files/fileops/{operationType} | Get file operation statuses |
| *FilesOperationsApi* | [**markAsRead**](#markasread) | **PUT** /api/2.0/files/fileops/markasread | Mark as read |
| *FilesOperationsApi* | [**moveBatchItems**](#movebatchitems) | **PUT** /api/2.0/files/fileops/move | Move or copy to a folder |
| *FilesOperationsApi* | [**startFileConversion**](#startfileconversion) | **PUT** /api/2.0/files/file/{fileId}/checkconversion | Start file conversion |
| *FilesOperationsApi* | [**terminateTasks**](#terminatetasks) | **PUT** /api/2.0/files/fileops/terminate/{id} | Finish active operations |
| *FilesOperationsApi* | [**updateFileComment**](#updatefilecomment) | **PUT** /api/2.0/files/file/{fileId}/comment | Update a comment |
| *FilesOperationsApi* | [**uploadAsyncSession**](#uploadasyncsession) | **POST** /api/2.0/files/{folderId}/session/{sessionId}/upload | Handles the upload of a chunk for an existing upload session. |
| *FilesOperationsApi* | [**uploadSession**](#uploadsession) | **POST** /api/2.0/files/{folderId}/session/{sessionId} | Resumes an ongoing file upload session for uploading additional chunks of data. |
| *FilesQuotaApi* | [**resetRoomQuota**](#resetroomquota) | **PUT** /api/2.0/files/rooms/resetquota | Reset the room quota limit |
| *FilesQuotaApi* | [**updateRoomsQuota**](#updateroomsquota) | **PUT** /api/2.0/files/rooms/roomquota | Change the room quota limit |
| *FilesSettingsApi* | [**changeAccessToThirdparty**](#changeaccesstothirdparty) | **PUT** /api/2.0/files/thirdparty | Change the third-party settings access |
| *FilesSettingsApi* | [**changeAutomaticallyCleanUp**](#changeautomaticallycleanup) | **PUT** /api/2.0/files/settings/autocleanup | Update the trash bin auto-clearing setting |
| *FilesSettingsApi* | [**changeDefaultAccessRights**](#changedefaultaccessrights) | **PUT** /api/2.0/files/settings/dafaultaccessrights | Change the default access rights |
| *FilesSettingsApi* | [**changeDeleteConfirm**](#changedeleteconfirm) | **PUT** /api/2.0/files/changedeleteconfrim | Confirm the file deletion |
| *FilesSettingsApi* | [**changeDownloadZip**](#changedownloadzip) | **PUT** /api/2.0/files/settings/downloadtargz | Change the archive format (using body parameters) |
| *FilesSettingsApi* | [**changeExternalSharingSettings**](#changeexternalsharingsettings) | **PUT** /api/2.0/files/settings/externalsharingsettings | Change the Access Control external sharing settings |
| *FilesSettingsApi* | [**checkDocServiceUrl**](#checkdocserviceurl) | **PUT** /api/2.0/files/docservice | Check the document service URL |
| *FilesSettingsApi* | [**displayFileExtension**](#displayfileextension) | **PUT** /api/2.0/files/displayfileextension | Display a file extension |
| *FilesSettingsApi* | [**displayRecent**](#displayrecent) | **PUT** /api/2.0/files/displayrecent | Display the Recent folder |
| *FilesSettingsApi* | [**externalShare**](#externalshare) | **PUT** /api/2.0/files/settings/external | Change the external sharing ability |
| *FilesSettingsApi* | [**externalShareSocialMedia**](#externalsharesocialmedia) | **PUT** /api/2.0/files/settings/externalsocialmedia | Change the external sharing ability on social networks |
| *FilesSettingsApi* | [**forcesave**](#forcesave) | **PUT** /api/2.0/files/forcesave | Change the forcesaving ability |
| *FilesSettingsApi* | [**getAutomaticallyCleanUp**](#getautomaticallycleanup) | **GET** /api/2.0/files/settings/autocleanup | Get the trash bin auto-clearing setting |
| *FilesSettingsApi* | [**getDefaultTemplates**](#getdefaulttemplates) | **GET** /api/2.0/files/settings/defaulttemplate | Get the default template setting |
| *FilesSettingsApi* | [**getDocServiceUrl**](#getdocserviceurl) | **GET** /api/2.0/files/docservice | Get the document service URL |
| *FilesSettingsApi* | [**getFilesModule**](#getfilesmodule) | **GET** /api/2.0/files/info | Get the Documents information |
| *FilesSettingsApi* | [**getFilesSettings**](#getfilessettings) | **GET** /api/2.0/files/settings | Get file settings |
| *FilesSettingsApi* | [**hideConfirmCancelOperation**](#hideconfirmcanceloperation) | **PUT** /api/2.0/files/hideconfirmcanceloperation | Hide confirmation dialog when canceling operations |
| *FilesSettingsApi* | [**hideConfirmConvert**](#hideconfirmconvert) | **PUT** /api/2.0/files/hideconfirmconvert | Hide the confirmation dialog when converting |
| *FilesSettingsApi* | [**hideConfirmRoomLifetime**](#hideconfirmroomlifetime) | **PUT** /api/2.0/files/hideconfirmroomlifetime | Hide confirmation dialog when changing room lifetime settings |
| *FilesSettingsApi* | [**keepNewFileName**](#keepnewfilename) | **PUT** /api/2.0/files/keepnewfilename | Ask a new file name |
| *FilesSettingsApi* | [**resetDefaultTemplate**](#resetdefaulttemplate) | **DELETE** /api/2.0/files/settings/defaulttemplate | Reset the default template setting |
| *FilesSettingsApi* | [**setDefaultTemplate**](#setdefaulttemplate) | **PUT** /api/2.0/files/settings/defaulttemplate | Change the default template setting |
| *FilesSettingsApi* | [**setOpenEditorInSameTab**](#setopeneditorinsametab) | **PUT** /api/2.0/files/settings/openeditorinsametab | Open document in the same browser tab |
| *FilesSettingsApi* | [**setOrganizeRoomsGrouping**](#setorganizeroomsgrouping) | **PUT** /api/2.0/files/settings/organizegrouping | Organize rooms grouping |
| *FilesSettingsApi* | [**storeForcesave**](#storeforcesave) | **PUT** /api/2.0/files/storeforcesave | Change the ability to store the forcesaved files |
| *FilesSettingsApi* | [**storeOriginal**](#storeoriginal) | **PUT** /api/2.0/files/storeoriginal | Change the ability to upload original formats |
| *FilesSettingsApi* | [**updateFileIfExist**](#updatefileifexist) | **PUT** /api/2.0/files/updateifexist | Update a file version if it exists |
| *FilesSettingsApi* | [**uploadDefaultTemplate**](#uploaddefaulttemplate) | **POST** /api/2.0/files/settings/defaulttemplate | Upload a file as the default template setting |
| *FilesSharingApi* | [**applyExternalSharePassword**](#applyexternalsharepassword) | **POST** /api/2.0/files/share/{key}/password | Apply external data password |
| *FilesSharingApi* | [**changeFileOwner**](#changefileowner) | **POST** /api/2.0/files/owner | Change the file owner |
| *FilesSharingApi* | [**getEncryptionAccess**](#getencryptionaccess) | **GET** /api/2.0/files/file/{fileId}/publickeys | Get file encryption keys |
| *FilesSharingApi* | [**getExternalShareData**](#getexternalsharedata) | **GET** /api/2.0/files/share/{key} | Get the external data |
| *FilesSharingApi* | [**getFileSecurityInfo**](#getfilesecurityinfo) | **GET** /api/2.0/files/file/{id}/share | Get the shared file information |
| *FilesSharingApi* | [**getFolderSecurityInfo**](#getfoldersecurityinfo) | **GET** /api/2.0/files/folder/{id}/share | Get the shared folder information |
| *FilesSharingApi* | [**getGroupsMembersWithFileSecurity**](#getgroupsmemberswithfilesecurity) | **GET** /api/2.0/files/file/{fileId}/group/{groupId}/share | Get file group members with security information |
| *FilesSharingApi* | [**getGroupsMembersWithFolderSecurity**](#getgroupsmemberswithfoldersecurity) | **GET** /api/2.0/files/folder/{folderId}/group/{groupId}/share | Get folder group members with security information |
| *FilesSharingApi* | [**getSecurityInfo**](#getsecurityinfo) | **POST** /api/2.0/files/share | Get the sharing rights |
| *FilesSharingApi* | [**getSharedUsers**](#getsharedusers) | **GET** /api/2.0/files/file/{fileId}/sharedusers | Get user access rights by file ID |
| *FilesSharingApi* | [**removeSecurityInfo**](#removesecurityinfo) | **DELETE** /api/2.0/files/share | Remove the sharing rights |
| *FilesSharingApi* | [**sendEditorNotify**](#sendeditornotify) | **POST** /api/2.0/files/file/{fileId}/sendeditornotify | Send the mention message |
| *FilesSharingApi* | [**setFileSecurityInfo**](#setfilesecurityinfo) | **PUT** /api/2.0/files/file/{fileId}/share | Share a file |
| *FilesSharingApi* | [**setFolderSecurityInfo**](#setfoldersecurityinfo) | **PUT** /api/2.0/files/folder/{folderId}/share | Share a folder |
| *FilesSharingApi* | [**setSecurityInfo**](#setsecurityinfo) | **PUT** /api/2.0/files/share | Set the sharing rights |
| *FilesThirdPartyIntegrationApi* | [**deleteThirdParty**](#deletethirdparty) | **DELETE** /api/2.0/files/thirdparty/{providerId} | Remove a third-party account |
| *FilesThirdPartyIntegrationApi* | [**getAllProviders**](#getallproviders) | **GET** /api/2.0/files/thirdparty/providers | Get all providers |
| *FilesThirdPartyIntegrationApi* | [**getBackupThirdPartyAccount**](#getbackupthirdpartyaccount) | **GET** /api/2.0/files/thirdparty/backup | Get a third-party account backup |
| *FilesThirdPartyIntegrationApi* | [**getCapabilities**](#getcapabilities) | **GET** /api/2.0/files/thirdparty/capabilities | Get providers |
| *FilesThirdPartyIntegrationApi* | [**getCommonThirdPartyFolders**](#getcommonthirdpartyfolders) | **GET** /api/2.0/files/thirdparty/common | Get the common third-party services |
| *FilesThirdPartyIntegrationApi* | [**getThirdPartyAccounts**](#getthirdpartyaccounts) | **GET** /api/2.0/files/thirdparty | Get the third-party accounts |
| *FilesThirdPartyIntegrationApi* | [**saveThirdParty**](#savethirdparty) | **POST** /api/2.0/files/thirdparty | Save a third-party account |
| *FilesThirdPartyIntegrationApi* | [**saveThirdPartyBackup**](#savethirdpartybackup) | **POST** /api/2.0/files/thirdparty/backup | Save a third-party account backup |
| *PrivacyroomApi* | [**deleteKeys**](#deletekeys) | **DELETE** /api/2.0/privacyroom/keys/{id} | Deletes an encryption key and removes it from the system. |
| *PrivacyroomApi* | [**getUserKeys**](#getuserkeys) | **GET** /api/2.0/privacyroom/keys | Retrieves encryption keys associated with the current user. |
| *PrivacyroomApi* | [**getUserKeysForRoom**](#getuserkeysforroom) | **GET** /api/2.0/privacyroom/{roomId}/access | Retrieves the encryption keys associated with a specific privacy room. |
| *PrivacyroomApi* | [**replaceKey**](#replacekey) | **PUT** /api/2.0/privacyroom/keys | Replaces an existing encryption key with a new one for the user. |
| *PrivacyroomApi* | [**setKeys**](#setkeys) | **POST** /api/2.0/privacyroom/keys | Creates and sets encryption keys for the user. |
| *RoomsApi* | [**addRoomTags**](#addroomtags) | **PUT** /api/2.0/files/rooms/{id}/tags | Add the room tags |
| *RoomsApi* | [**archiveRoom**](#archiveroom) | **PUT** /api/2.0/files/rooms/{id}/archive | Archive a room |
| *RoomsApi* | [**changeRoomCover**](#changeroomcover) | **POST** /api/2.0/files/rooms/{id}/cover | Change the room cover |
| *RoomsApi* | [**createRoom**](#createroom) | **POST** /api/2.0/files/rooms | Create a room |
| *RoomsApi* | [**createRoomFromTemplate**](#createroomfromtemplate) | **POST** /api/2.0/files/rooms/fromtemplate | Create a room from the template |
| *RoomsApi* | [**createRoomLogo**](#createroomlogo) | **POST** /api/2.0/files/rooms/{id}/logo | Create a room logo |
| *RoomsApi* | [**createRoomTag**](#createroomtag) | **POST** /api/2.0/files/tags | Create a room tag |
| *RoomsApi* | [**createRoomTemplate**](#createroomtemplate) | **POST** /api/2.0/files/roomtemplate | Start creating room template |
| *RoomsApi* | [**createRoomThirdParty**](#createroomthirdparty) | **POST** /api/2.0/files/rooms/thirdparty/{id} | Create a third-party room |
| *RoomsApi* | [**deleteCustomTags**](#deletecustomtags) | **DELETE** /api/2.0/files/tags | Delete the custom room tags |
| *RoomsApi* | [**deleteRoom**](#deleteroom) | **DELETE** /api/2.0/files/rooms/{id} | Remove a room |
| *RoomsApi* | [**deleteRoomLogo**](#deleteroomlogo) | **DELETE** /api/2.0/files/rooms/{id}/logo | Remove a room logo |
| *RoomsApi* | [**deleteRoomTags**](#deleteroomtags) | **DELETE** /api/2.0/files/rooms/{id}/tags | Remove the room tags |
| *RoomsApi* | [**getExternalDbSyncStatus**](#getexternaldbsyncstatus) | **GET** /api/2.0/files/rooms/{id}/externaldbsync | Get external DB sync status |
| *RoomsApi* | [**getNewRoomItems**](#getnewroomitems) | **GET** /api/2.0/files/rooms/{id}/news | Get the new room items |
| *RoomsApi* | [**getPublicSettings**](#getpublicsettings) | **GET** /api/2.0/files/roomtemplate/{id}/public | Get public settings |
| *RoomsApi* | [**getRoomCovers**](#getroomcovers) | **GET** /api/2.0/files/rooms/covers | Get covers |
| *RoomsApi* | [**getRoomCreatingStatus**](#getroomcreatingstatus) | **GET** /api/2.0/files/rooms/fromtemplate/status | Get the room creation progress |
| *RoomsApi* | [**getRoomIndexExport**](#getroomindexexport) | **GET** /api/2.0/files/rooms/indexexport | Get the room index export |
| *RoomsApi* | [**getRoomInfo**](#getroominfo) | **GET** /api/2.0/files/rooms/{id} | Get room information |
| *RoomsApi* | [**getRoomLinks**](#getroomlinks) | **GET** /api/2.0/files/rooms/{id}/links | Get the room links |
| *RoomsApi* | [**getRoomSecurityInfo**](#getroomsecurityinfo) | **GET** /api/2.0/files/rooms/{id}/share | Get the room access rights |
| *RoomsApi* | [**getRoomTagsInfo**](#getroomtagsinfo) | **GET** /api/2.0/files/tags | Get the room tags |
| *RoomsApi* | [**getRoomTemplateCreatingStatus**](#getroomtemplatecreatingstatus) | **GET** /api/2.0/files/roomtemplate/status | Get status of room template creation |
| *RoomsApi* | [**getRoomsFolder**](#getroomsfolder) | **GET** /api/2.0/files/rooms | Get rooms |
| *RoomsApi* | [**getRoomsNewItems**](#getroomsnewitems) | **GET** /api/2.0/files/rooms/news | Get the room new items |
| *RoomsApi* | [**getRoomsPrimaryExternalLink**](#getroomsprimaryexternallink) | **GET** /api/2.0/files/rooms/{id}/link | Get the room primary external link |
| *RoomsApi* | [**hasTagLinks**](#hastaglinks) | **GET** /api/2.0/files/tags/{tagName}/haslinks | Has tag links |
| *RoomsApi* | [**pinRoom**](#pinroom) | **PUT** /api/2.0/files/rooms/{id}/pin | Pin a room |
| *RoomsApi* | [**reorderRoom**](#reorderroom) | **PUT** /api/2.0/files/rooms/{id}/reorder | Reorder the room |
| *RoomsApi* | [**resendEmailInvitations**](#resendemailinvitations) | **POST** /api/2.0/files/rooms/{id}/resend | Resend the room invitations |
| *RoomsApi* | [**setPublicSettings**](#setpublicsettings) | **PUT** /api/2.0/files/roomtemplate/public | Set public settings |
| *RoomsApi* | [**setRoomLink**](#setroomlink) | **PUT** /api/2.0/files/rooms/{id}/links | Set the room external or invitation link |
| *RoomsApi* | [**setRoomSecurity**](#setroomsecurity) | **PUT** /api/2.0/files/rooms/{id}/share | Set the room access rights |
| *RoomsApi* | [**startExternalDbSync**](#startexternaldbsync) | **POST** /api/2.0/files/rooms/{id}/externaldbsync | Start external DB sync |
| *RoomsApi* | [**startRoomIndexExport**](#startroomindexexport) | **POST** /api/2.0/files/rooms/{id}/indexexport | Start the room index export |
| *RoomsApi* | [**terminateRoomIndexExport**](#terminateroomindexexport) | **DELETE** /api/2.0/files/rooms/indexexport | Terminate the room index export |
| *RoomsApi* | [**unarchiveRoom**](#unarchiveroom) | **PUT** /api/2.0/files/rooms/{id}/unarchive | Unarchive a room |
| *RoomsApi* | [**unpinRoom**](#unpinroom) | **PUT** /api/2.0/files/rooms/{id}/unpin | Unpin a room |
| *RoomsApi* | [**updateRoom**](#updateroom) | **PUT** /api/2.0/files/rooms/{id} | Update a room |
| *RoomsApi* | [**updateRoomTag**](#updateroomtag) | **PUT** /api/2.0/files/tags | Update tag |
| *RoomsApi* | [**uploadRoomLogo**](#uploadroomlogo) | **POST** /api/2.0/files/logos | Upload a room logo image |
| *RoomsGroupsApi* | [**addRoomGroup**](#addroomgroup) | **POST** /api/2.0/files/group | Add a new room group |
| *RoomsGroupsApi* | [**changeRoomGroupIcon**](#changeroomgroupicon) | **POST** /api/2.0/files/group/{id}/icon | Change group icon |
| *RoomsGroupsApi* | [**deleteRoomGroup**](#deleteroomgroup) | **DELETE** /api/2.0/files/group/{id} | Delete group |
| *RoomsGroupsApi* | [**getRoomGroupInfo**](#getroomgroupinfo) | **GET** /api/2.0/files/group/{id} | Get room group info |
| *RoomsGroupsApi* | [**getRoomGroups**](#getroomgroups) | **GET** /api/2.0/files/group | List room groups |
| *RoomsGroupsApi* | [**updateRoomGroup**](#updateroomgroup) | **PUT** /api/2.0/files/group/{id} | Update room group |



## FilesFilesApi

### addFileToRecent

> FileIntegerWrapper addFileToRecent(fileId)

`POST /api/2.0/files/file/{fileId}/recent`

Add a file to the Recent section

Adds a file with the ID specified in the request to the Recent section.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **fileId** | path | **Integer** (int32) | The file unique identifier. | [required] [example: 1] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | New file information | [**FileIntegerWrapper**](#model-fileintegerwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **403** | You don&#39;t have enough permission to perform the operation | - | - |
| **404** | File not found | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**FileIntegerWrapper**](#model-fileintegerwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### addTemplates

> BooleanWrapper addTemplates(TemplatesRequestDto)

`POST /api/2.0/files/templates`

Add template files

Adds files with the IDs specified in the request to the template list.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **TemplatesRequestDto** | body | [**TemplatesRequestDto**](#model-templatesrequestdto) |  | [optional] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Boolean value: true if the operation is successful | [**BooleanWrapper**](#model-booleanwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**BooleanWrapper**](#model-booleanwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

### changeVersionHistory

> FileIntegerArrayWrapper changeVersionHistory(fileId, ChangeHistory)

`PUT /api/2.0/files/file/{fileId}/history`

Change version history

Changes the version history of a file with the ID specified in the request.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **fileId** | path | **Integer** (int32) | The file Id to change its version history. | [required] [example: 1] |
| **ChangeHistory** | body | [**ChangeHistory**](#model-changehistory) | The parameters for changing version history. | [required] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Updated information about file versions | [**FileIntegerArrayWrapper**](#model-fileintegerarraywrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **403** | You do not have enough permissions to edit the file | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**FileIntegerArrayWrapper**](#model-fileintegerarraywrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

### checkFillFormDraft

> StringWrapper checkFillFormDraft(fileId, CheckFillFormDraft)

`POST /api/2.0/files/masterform/{fileId}/checkfillformdraft`

Check the form draft filling

Checks if the current file is a form draft which can be filled out.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **fileId** | path | **Integer** (int32) | The file ID of the form draft. | [required] [example: 1] |
| **CheckFillFormDraft** | body | [**CheckFillFormDraft**](#model-checkfillformdraft) | The parameters for checking the form draft filling. | [required] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Link to the form | [**StringWrapper**](#model-stringwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **403** | You don&#39;t have enough permission to view the file | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**StringWrapper**](#model-stringwrapper)

#### Authorization

No authorization required

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

### copyFileAs

> FileEntryBaseWrapper copyFileAs(fileId, CopyAsJsonElement)

`POST /api/2.0/files/file/{fileId}/copyas`

Copy a file

Copies (and converts if possible) an existing file to the specified folder.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **fileId** | path | **Integer** (int32) | The file ID to copy. | [required] [example: 1] |
| **CopyAsJsonElement** | body | [**CopyAsJsonElement**](#model-copyasjsonelement) | The parameters for copying a file. | [required] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Copied file entry information | [**FileEntryBaseWrapper**](#model-fileentrybasewrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **400** | No file id or folder id toFolderId determine provider | - | - |
| **403** | You don&#39;t have enough permission to create | - | - |
| **404** | File not found | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**FileEntryBaseWrapper**](#model-fileentrybasewrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

### createEditSession

> ChunkedUploadSessionResponseWrapperIntegerWrapper createEditSession(fileId, fileSize)

`POST /api/2.0/files/file/{fileId}/edit_session`

Create the editing session

Creates a session to edit the existing file with multiple chunks (needed for WebDAV).

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **fileId** | path | **Integer** (int32) | The file ID. | [required] [example: 1] |
| **fileSize** | query | **Long** (int64) | The file size in bytes. | [optional] [example: 1024] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Information about created session | [**ChunkedUploadSessionResponseWrapperIntegerWrapper**](#model-chunkeduploadsessionresponsewrapperintegerwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **403** | You don&#39;t have enough permission to edit the file | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**ChunkedUploadSessionResponseWrapperIntegerWrapper**](#model-chunkeduploadsessionresponsewrapperintegerwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### createFile

> FileIntegerWrapper createFile(folderId, CreateFileJsonElement)

`POST /api/2.0/files/{folderId}/file`

Create a file

Creates a new file in the specified folder with the title specified in the request.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **folderId** | path | **Integer** (int32) | The folder ID for the file creation. | [required] [example: 1] |
| **CreateFileJsonElement** | body | [**CreateFileJsonElement**](#model-createfilejsonelement) | The parameters for creating a file. | [required] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | New file information | [**FileIntegerWrapper**](#model-fileintegerwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**FileIntegerWrapper**](#model-fileintegerwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

### createFileInMyDocuments

> FileIntegerWrapper createFileInMyDocuments(CreateFileJsonElement)

`POST /api/2.0/files/@my/file`

Create a file in the My documents section

Creates a new file in the My documents section with the title specified in the request.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **CreateFileJsonElement** | body | [**CreateFileJsonElement**](#model-createfilejsonelement) |  | [optional] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | New file information | [**FileIntegerWrapper**](#model-fileintegerwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**FileIntegerWrapper**](#model-fileintegerwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

### createFilePrimaryExternalLink

> FileShareWrapper createFilePrimaryExternalLink(id, FileLinkRequest)

`POST /api/2.0/files/file/{id}/link`

Create primary external link

Creates a primary external link by the identifier specified in the request.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **id** | path | **Integer** (int32) | The file ID. | [required] [example: 1] |
| **FileLinkRequest** | body | [**FileLinkRequest**](#model-filelinkrequest) | The file external link parameters. | [required] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | File security information | [**FileShareWrapper**](#model-filesharewrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **403** | You don&#39;t have enough permission to perform the operation | - | - |
| **404** | Not Found | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**FileShareWrapper**](#model-filesharewrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

### createHtmlFile

> FileIntegerWrapper createHtmlFile(folderId, CreateTextOrHtmlFile)

`POST /api/2.0/files/{folderId}/html`

Create an HTML file

Creates an HTML (.html) file in the selected folder with the title and contents specified in the request.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **folderId** | path | **Integer** (int32) | The folder ID to create the text or HTML file. | [required] [example: 1] |
| **CreateTextOrHtmlFile** | body | [**CreateTextOrHtmlFile**](#model-createtextorhtmlfile) | The parameters for creating an HTML or text file. | [required] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | New file information | [**FileIntegerWrapper**](#model-fileintegerwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **403** | You don&#39;t have enough permission to create | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**FileIntegerWrapper**](#model-fileintegerwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

### createHtmlFileInMyDocuments

> FileIntegerWrapper createHtmlFileInMyDocuments(CreateTextOrHtmlFile)

`POST /api/2.0/files/@my/html`

Create an HTML file in the My documents section

Creates an HTML (.html) file in the My documents section with the title and contents specified in the request.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **CreateTextOrHtmlFile** | body | [**CreateTextOrHtmlFile**](#model-createtextorhtmlfile) |  | [optional] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | New file information | [**FileIntegerWrapper**](#model-fileintegerwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **403** | You don&#39;t have enough permission to create | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**FileIntegerWrapper**](#model-fileintegerwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

### createTextFile

> FileIntegerWrapper createTextFile(folderId, CreateTextOrHtmlFile)

`POST /api/2.0/files/{folderId}/text`

Create a text file

Creates a text (.txt) file in the selected folder with the title and contents specified in the request.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **folderId** | path | **Integer** (int32) | The folder ID to create the text or HTML file. | [required] [example: 1] |
| **CreateTextOrHtmlFile** | body | [**CreateTextOrHtmlFile**](#model-createtextorhtmlfile) | The parameters for creating an HTML or text file. | [required] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | New file information | [**FileIntegerWrapper**](#model-fileintegerwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**FileIntegerWrapper**](#model-fileintegerwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

### createTextFileInMyDocuments

> FileIntegerWrapper createTextFileInMyDocuments(CreateTextOrHtmlFile)

`POST /api/2.0/files/@my/text`

Create a text file in the My documents section

Creates a text (.txt) file in the My documents section with the title and contents specified in the request.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **CreateTextOrHtmlFile** | body | [**CreateTextOrHtmlFile**](#model-createtextorhtmlfile) |  | [optional] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | New file information | [**FileIntegerWrapper**](#model-fileintegerwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**FileIntegerWrapper**](#model-fileintegerwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

### createThumbnails

> ObjectArrayWrapper createThumbnails(BaseBatchRequestDto)

`POST /api/2.0/files/thumbnails`

Create file thumbnails

Creates thumbnails for the files with the IDs specified in the request.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **BaseBatchRequestDto** | body | [**BaseBatchRequestDto**](#model-basebatchrequestdto) |  | [optional] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | List of file IDs | [**ObjectArrayWrapper**](#model-objectarraywrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**ObjectArrayWrapper**](#model-objectarraywrapper)

#### Authorization

No authorization required

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

### deleteFile

> FileOperationArrayWrapper deleteFile(fileId, Delete, ReturnSingleOperation)

`DELETE /api/2.0/files/file/{fileId}`

Delete a file

Deletes a file with the ID specified in the request.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **fileId** | path | **Integer** (int32) | The file ID to delete. | [required] [example: 1] |
| **Delete** | body | [**Delete**](#model-delete) | The parameters for deleting a file. | [required] |
| **ReturnSingleOperation** | query | **Boolean** | Specifies whether to return only the current operation | [optional] [example: false] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | List of file operations | [**FileOperationArrayWrapper**](#model-fileoperationarraywrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**FileOperationArrayWrapper**](#model-fileoperationarraywrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

### deleteRecent

> NoContentResultWrapper deleteRecent(BaseBatchRequestDto)

`DELETE /api/2.0/files/recent`

Delete recent files

Removes files with the IDs specified in the request from the Recent section.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **BaseBatchRequestDto** | body | [**BaseBatchRequestDto**](#model-basebatchrequestdto) |  | [optional] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | No content | [**NoContentResultWrapper**](#model-nocontentresultwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**NoContentResultWrapper**](#model-nocontentresultwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

### deleteTemplates

> BooleanWrapper deleteTemplates(request\_body)

`DELETE /api/2.0/files/templates`

Delete template files

Removes files with the IDs specified in the request from the template list.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **request\_body** | body | **List** | The file IDs. | [optional] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Boolean value: true if the operation is successful | [**BooleanWrapper**](#model-booleanwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**BooleanWrapper**](#model-booleanwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

### generateXlsx

> XlsxReportResponseWrapper generateXlsx(fileId)

`POST /api/2.0/files/file/{fileId}/xlsx`

Generate XLSX report

Triggers asynchronous XLSX report generation for the specified form file.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **fileId** | path | **Integer** (int32) | The file unique identifier. | [required] [example: 1] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Ok | [**XlsxReportResponseWrapper**](#model-xlsxreportresponsewrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **403** | You do not have enough permissions to perform this action | - | - |
| **404** | The required file was not found | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**XlsxReportResponseWrapper**](#model-xlsxreportresponsewrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### getAllFormRoles

> FormRoleArrayWrapper getAllFormRoles(fileId)

`GET /api/2.0/files/file/{fileId}/formroles`

Get form roles

Returns all roles for the specified form.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **fileId** | path | **Integer** (int32) | The file unique identifier. | [required] [example: 1] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Successfully retrieved all roles for the form | [**FormRoleArrayWrapper**](#model-formrolearraywrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **403** | You do not have enough permissions to view the form roles | - | - |
| **404** | The required file was not found | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**FormRoleArrayWrapper**](#model-formrolearraywrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### getEditDiffUrl

> EditHistoryDataWrapper getEditDiffUrl(fileId, version)

`GET /api/2.0/files/file/{fileId}/edit/diff`

Get changes URL

Returns a URL to the changes of a file version specified in the request.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **fileId** | path | **Integer** (int32) | The file ID. | [required] [example: 1] |
| **version** | query | **Integer** (int32) | The file version. | [optional] [example: 1] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | File version history data | [**EditHistoryDataWrapper**](#model-edithistorydatawrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**EditHistoryDataWrapper**](#model-edithistorydatawrapper)

#### Authorization

No authorization required

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### getEditHistory

> EditHistoryArrayWrapper getEditHistory(fileId)

`GET /api/2.0/files/file/{fileId}/edit/history`

Get version history

Returns the version history of a file with the ID specified in the request.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **fileId** | path | **Integer** (int32) | The file unique identifier. | [required] [example: 1] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Version history data | [**EditHistoryArrayWrapper**](#model-edithistoryarraywrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**EditHistoryArrayWrapper**](#model-edithistoryarraywrapper)

#### Authorization

No authorization required

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### getEncryptionInfo

> FileEncryptionInfoWrapper getEncryptionInfo(fileId)

`GET /api/2.0/files/{fileId}/access`

Get file encryption information

Returns the encryption information for a file with the specified identifier, including user encryption keys and file-specific encryption keys.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **fileId** | path | **Integer** (int32) |  | [required] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | File encryption information | [**FileEncryptionInfoWrapper**](#model-fileencryptioninfowrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **400** | Invalid operation | - | - |
| **403** | You don&#39;t have enough permission to read the file | - | - |
| **404** | File not found | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**FileEncryptionInfoWrapper**](#model-fileencryptioninfowrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### getFileHistory

> HistoryArrayWrapper getFileHistory(fileId, fromDate, toDate, count, startIndex)

`GET /api/2.0/files/file/{fileId}/log`

Get file history

Returns the list of actions performed on the file with the specified identifier.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **fileId** | path | **Integer** (int32) | The file ID of the history request. | [required] [example: 1] |
| **fromDate** | query | **ApiDateTime** | The start date of the history. | [optional] [example: 2025-01-01T00:00:00.0000000Z] |
| **toDate** | query | **ApiDateTime** | The end date of the history. | [optional] [example: 2025-12-31T23:59:59.0000000Z] |
| **count** | query | **Integer** (int32) | The number of history entries to retrieve for the file log. | [optional] [example: 25] [min: 1] [max: 100] |
| **startIndex** | query | **Integer** (int32) | The starting index for retrieving a subset of file history entries. | [optional] [example: 0] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | List of actions performed on the file | [**HistoryArrayWrapper**](#model-historyarraywrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **403** | You don&#39;t have enough permission to perform the operation | - | - |
| **404** | The required file was not found | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**HistoryArrayWrapper**](#model-historyarraywrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### getFileInfo

> FileIntegerWrapper getFileInfo(fileId, version)

`GET /api/2.0/files/file/{fileId}`

Get file information

Returns the detailed information about a file with the ID specified in the request.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **fileId** | path | **Integer** (int32) | The file ID. | [required] [example: 1] |
| **version** | query | **Integer** (int32) | The file version. | [optional] [example: 1] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | File information | [**FileIntegerWrapper**](#model-fileintegerwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**FileIntegerWrapper**](#model-fileintegerwrapper)

#### Authorization

No authorization required

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### getFileLinks

> FileShareArrayWrapper getFileLinks(id, count, startIndex)

`GET /api/2.0/files/file/{id}/links`

Get file external links

Returns the external links of a file with the ID specified in the request.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **id** | path | **Integer** (int32) | The file unique identifier. | [required] [example: 10] |
| **count** | query | **Integer** (int32) | The number of items to retrieve in the request. | [optional] [example: 25] [min: 1] [max: 100] |
| **startIndex** | query | **Integer** (int32) | The starting index for the query results. | [optional] [example: 0] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | File security information | [**FileShareArrayWrapper**](#model-filesharearraywrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**FileShareArrayWrapper**](#model-filesharearraywrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### getFilePrimaryExternalLink

> FileShareWrapper getFilePrimaryExternalLink(id, count, startIndex)

`GET /api/2.0/files/file/{id}/link`

Get primary external link

Returns the primary external link by the identifier specified in the request.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **id** | path | **Integer** (int32) | The file unique identifier. | [required] [example: 10] |
| **count** | query | **Integer** (int32) | The number of items to retrieve in the request. | [optional] [example: 25] [min: 1] [max: 100] |
| **startIndex** | query | **Integer** (int32) | The starting index for the query results. | [optional] [example: 0] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | File security information | [**FileShareWrapper**](#model-filesharewrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **403** | You don&#39;t have enough permission to perform the operation | - | - |
| **404** | Not Found | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**FileShareWrapper**](#model-filesharewrapper)

#### Authorization

No authorization required

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### getFileVersionInfo

> FileIntegerArrayWrapper getFileVersionInfo(fileId)

`GET /api/2.0/files/file/{fileId}/history`

Get file versions

Returns the detailed information about all the available file versions with the ID specified in the request.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **fileId** | path | **Integer** (int32) | The file unique identifier. | [required] [example: 1] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Information about file versions: folder ID, version, version group, content length, pure content length, file status, URL to view a file, web URL, file type, file extension, comment, encrypted or not, thumbnail URL, thumbnail status, locked or not, user ID who locked a file, denies file downloading or not, denies file sharing or not, file accessibility | [**FileIntegerArrayWrapper**](#model-fileintegerarraywrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**FileIntegerArrayWrapper**](#model-fileintegerarraywrapper)

#### Authorization

No authorization required

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### getFillResult

> FillingFormResultIntegerWrapper getFillResult(fillingSessionId)

`GET /api/2.0/files/file/fillresult`

Get form-filling result

Retrieves the result of a form-filling session.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **fillingSessionId** | query | **String** | The form-filling session ID. | [optional] [example: doc_key_123] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Ok | [**FillingFormResultIntegerWrapper**](#model-fillingformresultintegerwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**FillingFormResultIntegerWrapper**](#model-fillingformresultintegerwrapper)

#### Authorization

No authorization required

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### getFormSubmissions

> FormSubmissionsWrapper getFormSubmissions(fileId)

`GET /api/2.0/files/file/{fileId}/submissions`

Get form submission results

Returns the results of form submissions.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **fileId** | path | **Integer** (int32) | The file unique identifier. | [required] [example: 1] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Form submission results were successfully retrieved | [**FormSubmissionsWrapper**](#model-formsubmissionswrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **403** | You do not have enough permissions to perform this action | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**FormSubmissionsWrapper**](#model-formsubmissionswrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### getPresignedFileUri

> FileLinkWrapper getPresignedFileUri(fileId)

`GET /api/2.0/files/file/{fileId}/presigned`

Get file download link asynchronously

Returns a link to download a file with the ID specified in the request asynchronously.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **fileId** | path | **Integer** (int32) | The file unique identifier. | [required] [example: 1] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | File download link | [**FileLinkWrapper**](#model-filelinkwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**FileLinkWrapper**](#model-filelinkwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### getPresignedUri

> StringWrapper getPresignedUri(fileId)

`GET /api/2.0/files/file/{fileId}/presigneduri`

Get file download link

Returns a pre-signed URL to download a file with the specified ID.  This temporary link provides secure access to the file.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **fileId** | path | **Integer** (int32) | The file unique identifier. | [required] [example: 1] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | File download link | [**StringWrapper**](#model-stringwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**StringWrapper**](#model-stringwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### getProtectedFileUsers

> MentionWrapperArrayWrapper getProtectedFileUsers(fileId)

`GET /api/2.0/files/file/{fileId}/protectusers`

Get users access rights to the protected file

Returns a list of users with their access rights to the protected file with the ID specified in the request.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **fileId** | path | **Integer** (int32) | The file unique identifier. | [required] [example: 1] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | List of users with their access rights to the protected file | [**MentionWrapperArrayWrapper**](#model-mentionwrapperarraywrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**MentionWrapperArrayWrapper**](#model-mentionwrapperarraywrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### getReferenceData

> FileReferenceWrapper getReferenceData(GetReferenceDataDtoInteger)

`POST /api/2.0/files/file/referencedata`

Get reference data

Returns the reference data to uniquely identify a file in its system and check the availability of insering data into the destination spreadsheet by the external link.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **GetReferenceDataDtoInteger** | body | [**GetReferenceDataDtoInteger**](#model-getreferencedatadtointeger) |  | [optional] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | File reference data | [**FileReferenceWrapper**](#model-filereferencewrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**FileReferenceWrapper**](#model-filereferencewrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

### getXlsx

> DocumentBuilderTaskWrapper getXlsx(fileId)

`GET /api/2.0/files/file/{fileId}/xlsx`

Get XLSX report generation status

Returns the status of the XLSX report generation task for the specified form.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **fileId** | path | **Integer** (int32) | The file unique identifier. | [required] [example: 1] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Ok | [**DocumentBuilderTaskWrapper**](#model-documentbuildertaskwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**DocumentBuilderTaskWrapper**](#model-documentbuildertaskwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### isFormPDF

> BooleanWrapper isFormPDF(fileId)

`GET /api/2.0/files/file/{fileId}/isformpdf`

Check the PDF file

Checks if the PDF file is a form or not.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **fileId** | path | **Integer** (int32) | The file unique identifier. | [required] [example: 1] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Boolean value: true - the PDF file is form, false - the PDF file is not a form | [**BooleanWrapper**](#model-booleanwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**BooleanWrapper**](#model-booleanwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### lockFile

> FileIntegerWrapper lockFile(fileId, LockFileParameters)

`PUT /api/2.0/files/file/{fileId}/lock`

Lock a file

Locks a file with the ID specified in the request.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **fileId** | path | **Integer** (int32) | The file ID for locking. | [required] [example: 1] |
| **LockFileParameters** | body | [**LockFileParameters**](#model-lockfileparameters) | The parameters for locking a file. | [required] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Locked file information | [**FileIntegerWrapper**](#model-fileintegerwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**FileIntegerWrapper**](#model-fileintegerwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

### manageFormFilling

> manageFormFilling(fileId, ManageFormFillingDtoInteger)

`PUT /api/2.0/files/file/{fileId}/manageformfilling`

Perform form filling action

Performs the specified form filling action.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **fileId** | path | **String** |  | [required] |
| **ManageFormFillingDtoInteger** | body | [**ManageFormFillingDtoInteger**](#model-manageformfillingdtointeger) |  | [optional] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Successfully processed the form filling action | - | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **403** | You do not have enough permissions to perform this action | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

null (empty response body)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: Not defined

### openEditFile

> ConfigurationIntegerWrapper openEditFile(fileId, version, view, editorType, edit, fill)

`GET /api/2.0/files/file/{fileId}/openedit`

Open a file configuration

Returns the initialization configuration of a file to open it in the editor.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **fileId** | path | **Integer** (int32) | The file ID to open. | [required] [example: 1] |
| **version** | query | **Integer** (int32) | The file version to open. | [optional] [example: 1] |
| **view** | query | **Boolean** | Specifies if the document will be opened for viewing only or not. | [optional] [example: false] |
| **editorType** | query | **EditorType** | The editor type to open the file. | [optional] [example: 1] [enum: 0, 1, 2] |
| **edit** | query | **Boolean** | Specifies if the document is opened in the editing mode or not. | [optional] [example: false] |
| **fill** | query | **Boolean** | Specifies if the document is opened in the form-filling mode or not. | [optional] [example: false] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Configuration parameters | [**ConfigurationIntegerWrapper**](#model-configurationintegerwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **403** | You don&#39;t have enough permission to view the file | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**ConfigurationIntegerWrapper**](#model-configurationintegerwrapper)

#### Authorization

No authorization required

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### restoreFileVersion

> EditHistoryArrayWrapper restoreFileVersion(fileId, version, url)

`POST /api/2.0/files/file/{fileId}/restoreversion`

Restore a file version

Restores a file version specified in the request.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **fileId** | path | **Integer** (int32) | The file ID of the restore version. | [required] [example: 1] |
| **version** | query | **Integer** (int32) | The file version of the restore. | [optional] [example: 1] |
| **url** | query | **String** | The file version URL of the restore. | [optional] [example: https://example.com] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Version history data: file ID, key, file version, version group, a user who updated a file, creation time, history changes in the string format, list of history changes, server version | [**EditHistoryArrayWrapper**](#model-edithistoryarraywrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **400** | No file id or folder id toFolderId determine provider | - | - |
| **403** | You do not have enough permissions to edit the file | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**EditHistoryArrayWrapper**](#model-edithistoryarraywrapper)

#### Authorization

No authorization required

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### saveEditingFileFromForm

> FileIntegerWrapper saveEditingFileFromForm(fileId, DownloadUri, FileExtension, File, Forcesave)

`PUT /api/2.0/files/file/{fileId}/saveediting`

Save file edits

Saves edits to a file with the ID specified in the request.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **fileId** | path | **Integer** (int32) | The editing file ID from the request. | [required] [example: 1] |
| **DownloadUri** | query | **String** | The URI to download the editing file. | [optional] [example: https://example.com/file.txt] |
| **FileExtension** | form | **String** | The editing file extension from the request. | [optional] |
| **File** | form | **File** (binary) | The edited file to be saved, uploaded as part of the multipart/form-data request.  This property represents the modified file content from the HTTP request form after editing operations.  The file is accessed via the IFormFile interface which provides access to the file name, content type, length, and stream. | [optional] |
| **Forcesave** | form | **Boolean** | Specifies whether to force save the file or not. | [optional] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Saved file parameters | [**FileIntegerWrapper**](#model-fileintegerwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **400** | No file id or folder id toFolderId determine provider | - | - |
| **403** | You do not have enough permissions to edit the file | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**FileIntegerWrapper**](#model-fileintegerwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: multipart/form-data
- **Accept**: application/json

### saveFileAsPdf

> FileIntegerWrapper saveFileAsPdf(id, SaveAsPdfInteger)

`POST /api/2.0/files/file/{id}/saveaspdf`

Save a file as PDF

Saves a file with the identifier specified in the request as a PDF document.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **id** | path | **Integer** (int32) | The file ID to save as PDF. | [required] [example: 1] |
| **SaveAsPdfInteger** | body | [**SaveAsPdfInteger**](#model-saveaspdfinteger) | The parameters for saving the file as PDF. | [required] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | New file information | [**FileIntegerWrapper**](#model-fileintegerwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **404** | File not found | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**FileIntegerWrapper**](#model-fileintegerwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

### saveFormRoleMapping

> saveFormRoleMapping(fileId, SaveFormRoleMappingDtoInteger)

`POST /api/2.0/files/file/{fileId}/formrolemapping`

Save form role mapping

Saves the form role mapping.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **fileId** | path | **String** |  | [required] |
| **SaveFormRoleMappingDtoInteger** | body | [**SaveFormRoleMappingDtoInteger**](#model-saveformrolemappingdtointeger) |  | [optional] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Updated information about form role mappings | - | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **403** | You do not have enough permissions to edit the file | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

null (empty response body)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: Not defined

### setCustomFilterTag

> FileIntegerWrapper setCustomFilterTag(fileId, CustomFilterParameters)

`PUT /api/2.0/files/file/{fileId}/customfilter`

Set the Custom Filter editing mode

Sets the Custom Filter editing mode to a file with the ID specified in the request.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **fileId** | path | **Integer** (int32) | The file ID. | [required] [example: 1] |
| **CustomFilterParameters** | body | [**CustomFilterParameters**](#model-customfilterparameters) | The parameters for setting the Custom Filter editing mode. | [required] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | File information | [**FileIntegerWrapper**](#model-fileintegerwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**FileIntegerWrapper**](#model-fileintegerwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

### setEncryptionInfo

> setEncryptionInfo(fileId, AccessRequestKeyDto)

`PUT /api/2.0/files/{fileId}/access`

Set file encryption information

Sets or updates the encryption keys for a file with the specified identifier. This allows updating the file&#39;s encryption configuration.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **fileId** | path | **Integer** (int32) | File ID | [required] [example: 12345] |
| **AccessRequestKeyDto** | body | [**List**](#model-accessrequestkeydto) | Collection of encryption key data for users with access to the file | [optional] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Encryption information successfully updated | - | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **403** | You don&#39;t have enough permission to edit the file | - | - |
| **404** | File not found | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

null (empty response body)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: Not defined

### setFileExternalLink

> FileShareWrapper setFileExternalLink(id, FileLinkRequest)

`PUT /api/2.0/files/file/{id}/links`

Set an external link

Sets an external link to a file with the ID specified in the request.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **id** | path | **Integer** (int32) | The file ID. | [required] [example: 1] |
| **FileLinkRequest** | body | [**FileLinkRequest**](#model-filelinkrequest) | The file external link parameters. | [required] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | File security information | [**FileShareWrapper**](#model-filesharewrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**FileShareWrapper**](#model-filesharewrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

### setFileOrder

> FileIntegerWrapper setFileOrder(fileId, OrderRequestDto)

`PUT /api/2.0/files/{fileId}/order`

Set file order

Sets the order of the file with the ID specified in the request.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **fileId** | path | **Integer** (int32) | The file unique identifier. | [required] [example: 1] |
| **OrderRequestDto** | body | [**OrderRequestDto**](#model-orderrequestdto) | The file order information. | [optional] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Updated file information | [**FileIntegerWrapper**](#model-fileintegerwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **403** | You don&#39;t have enough permission to perform the operation | - | - |
| **404** | Not Found | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**FileIntegerWrapper**](#model-fileintegerwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

### setFilesOrder

> FileEntryIntegerArrayWrapper setFilesOrder(OrdersRequestDtoInteger)

`PUT /api/2.0/files/order`

Set order of files

Sets the order of the files specified in the request.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **OrdersRequestDtoInteger** | body | [**OrdersRequestDtoInteger**](#model-ordersrequestdtointeger) |  | [optional] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Updated file entries information | [**FileEntryIntegerArrayWrapper**](#model-fileentryintegerarraywrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**FileEntryIntegerArrayWrapper**](#model-fileentryintegerarraywrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

### startEditFile

> StringWrapper startEditFile(fileId, StartEdit)

`POST /api/2.0/files/file/{fileId}/startedit`

Start file editing

Informs about opening a file with the ID specified in the request for editing, locking it from being deleted or moved (this method is called by the mobile editors).

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **fileId** | path | **Integer** (int32) | The file ID to start editing. | [required] [example: 1] |
| **StartEdit** | body | [**StartEdit**](#model-startedit) | The file parameters to start editing. | [required] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | File key for Document Service | [**StringWrapper**](#model-stringwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **403** | You don&#39;t have enough permission to view the file | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**StringWrapper**](#model-stringwrapper)

#### Authorization

No authorization required

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

### startFillingFile

> FileIntegerWrapper startFillingFile(fileId)

`PUT /api/2.0/files/file/{fileId}/startfilling`

Start file filling

Starts filling a file with the ID specified in the request.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **fileId** | path | **Integer** (int32) | The file ID to start filling. | [required] [example: 1] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | File information | [**FileIntegerWrapper**](#model-fileintegerwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **403** | You do not have enough permissions to edit the file | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**FileIntegerWrapper**](#model-fileintegerwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### toggleFileFavorite

> BooleanWrapper toggleFileFavorite(fileId, favorite)

`GET /api/2.0/files/favorites/{fileId}`

Change the file favorite status

Changes the favorite status of the file with the ID specified in the request.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **fileId** | path | **Integer** (int32) | The file ID. | [required] [example: 1] |
| **favorite** | query | **Boolean** | Specifies if the file is marked as favorite or not. | [optional] [example: true] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Boolean value: true - the file is favorite, false - the file is not favorite | [**BooleanWrapper**](#model-booleanwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **403** | You don&#39;t have enough permission to perform the operation | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**BooleanWrapper**](#model-booleanwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### trackEditFile

> KeyValuePairBooleanStringWrapper trackEditFile(fileId, tabId, docKeyForTrack, isFinish)

`GET /api/2.0/files/file/{fileId}/trackeditfile`

Track file editing

Tracks file changes when editing.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **fileId** | path | **Integer** (int32) | The file ID to track editing changes. | [required] [example: 1] |
| **tabId** | query | **UUID** (uuid) | The tab ID to track editing changes. | [optional] [example: 00000000-0000-0000-0000-000000000000] |
| **docKeyForTrack** | query | **String** | The document key for tracking changes. | [optional] [example: abc123] |
| **isFinish** | query | **Boolean** | Specifies whether to finish file tracking or not. | [optional] [example: true] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | File changes | [**KeyValuePairBooleanStringWrapper**](#model-keyvaluepairbooleanstringwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **403** | You don&#39;t have enough permission to perform the operation | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**KeyValuePairBooleanStringWrapper**](#model-keyvaluepairbooleanstringwrapper)

#### Authorization

No authorization required

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### updateFile

> FileIntegerWrapper updateFile(fileId, UpdateFile)

`PUT /api/2.0/files/file/{fileId}`

Update a file

Updates the information of the selected file with the parameters specified in the request.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **fileId** | path | **Integer** (int32) | The file ID to update. | [required] [example: 1] |
| **UpdateFile** | body | [**UpdateFile**](#model-updatefile) | The parameters for updating a file. | [required] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Updated file information | [**FileIntegerWrapper**](#model-fileintegerwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **403** | You do not have enough permissions to edit the file | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**FileIntegerWrapper**](#model-fileintegerwrapper)

#### Authorization

No authorization required

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

## FilesFoldersApi

### checkUpload

> STRINGArrayWrapper checkUpload(folderId, CheckUploadRequest)

`POST /api/2.0/files/{folderId}/upload/check`

Check file uploads

Checks the file uploads to the folder with the ID specified in the request.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **folderId** | path | **Integer** (int32) | The folder ID. | [required] [example: 1] |
| **CheckUploadRequest** | body | [**CheckUploadRequest**](#model-checkuploadrequest) | The request parameters for checking file uploads. | [required] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Inserted file | [**STRINGArrayWrapper**](#model-stringarraywrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**STRINGArrayWrapper**](#model-stringarraywrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

### createFolder

> FolderIntegerWrapper createFolder(folderId, CreateFolder)

`POST /api/2.0/files/folder/{folderId}`

Create a folder

Creates a new folder with the title specified in the request. The parent folder ID can be also specified.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **folderId** | path | **Integer** (int32) | The folder ID for the folder creation. | [required] [example: 1] |
| **CreateFolder** | body | [**CreateFolder**](#model-createfolder) | The parameters for creating a folder. | [required] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | New folder parameters | [**FolderIntegerWrapper**](#model-folderintegerwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**FolderIntegerWrapper**](#model-folderintegerwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

### createFolderPrimaryExternalLink

> FileShareWrapper createFolderPrimaryExternalLink(id, FolderLinkRequest)

`POST /api/2.0/files/folder/{id}/link`

Create primary external link

Creates a primary external link by the identifier specified in the request.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **id** | path | **Integer** (int32) | The folder ID. | [required] [example: 1] |
| **FolderLinkRequest** | body | [**FolderLinkRequest**](#model-folderlinkrequest) | The folder link parameters. | [required] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Folders security information | [**FileShareWrapper**](#model-filesharewrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **403** | You don&#39;t have enough permission to perform the operation | - | - |
| **404** | Not Found | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**FileShareWrapper**](#model-filesharewrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

### createReportFolderHistory

> DocumentBuilderTaskWrapper createReportFolderHistory(folderId, format, from, to)

`POST /api/2.0/files/folder/{folderId}/log/report`

Start the folder history report generation

Starts generating the activity history report of a folder (XLSX by default, or CSV) and saves it to My documents.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **folderId** | path | **Integer** (int32) | The folder ID whose history is exported. | [required] [example: 1] |
| **format** | query | **AuditReportFormat** | The output file format of the report. Defaults to XLSX. | [optional] [example: Xlsx] [enum: 0, 1] |
| **from** | query | **ApiDateTime** | The start date of the history period to export. | [optional] [example: 2025-01-01T00:00:00.0000000Z] |
| **to** | query | **ApiDateTime** | The end date of the history period to export. | [optional] [example: 2025-12-31T23:59:59.0000000Z] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Operation execution status | [**DocumentBuilderTaskWrapper**](#model-documentbuildertaskwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **403** | You don&#39;t have enough permission to perform the operation | - | - |
| **404** | The required folder was not found | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**DocumentBuilderTaskWrapper**](#model-documentbuildertaskwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### deleteFolder

> FileOperationArrayWrapper deleteFolder(folderId, DeleteFolder)

`DELETE /api/2.0/files/folder/{folderId}`

Delete a folder

Deletes a folder with the ID specified in the request.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **folderId** | path | **Integer** (int32) | The folder ID to delete. | [required] [example: 10] |
| **DeleteFolder** | body | [**DeleteFolder**](#model-deletefolder) | The parameters for deleting a folder. | [required] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | List of file operations | [**FileOperationArrayWrapper**](#model-fileoperationarraywrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**FileOperationArrayWrapper**](#model-fileoperationarraywrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

### generateXlsxByFolder

> XlsxReportResponseWrapper generateXlsxByFolder(folderId)

`POST /api/2.0/files/folder/{folderId}/xlsx`

Generate XLSX report by folder

Triggers asynchronous XLSX report generation for the specified form results folder.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **folderId** | path | **Integer** (int32) | The folder unique identifier. | [required] [example: 1] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Ok | [**XlsxReportResponseWrapper**](#model-xlsxreportresponsewrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **403** | You do not have enough permissions to perform this action | - | - |
| **404** | The required folder was not found | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**XlsxReportResponseWrapper**](#model-xlsxreportresponsewrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### getFavoritesFolder

> FolderContentIntegerWrapper getFavoritesFolder(userIdOrGroupId, filterType, count, startIndex, sortBy, sortOrder, filterValue)

`GET /api/2.0/files/@favorites`

Get the Favorites section

Returns the detailed list of files and folders located in the Favorites section.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **userIdOrGroupId** | query | **UUID** (uuid) | The user or group ID. | [optional] [example: 00000000-0000-0000-0000-000000000000] |
| **filterType** | query | **FilterType** | The filter type. | [optional] [example: 1] [enum: 0, 1, 2, 3, 4, 5, 7, 8, 9, 10, 11, 12, 13, 14, 17, 20, 22, 23, 24, 25, 26] |
| **count** | query | **Integer** (int32) | The maximum number of items to retrieve in the request. | [optional] [example: 25] [min: 1] [max: 100] |
| **startIndex** | query | **Integer** (int32) | The zero-based index of the first item to retrieve in a paginated list. | [optional] [example: 0] |
| **sortBy** | query | **String** | Specifies the field by which the folder content should be sorted. | [optional] [example: DateAndTime] |
| **sortOrder** | query | **SortOrder** | The order in which the results are sorted. | [optional] [example: 1] [enum: 0, 1] |
| **filterValue** | query | **String** | The text used as a filter or search criterion for folder content queries. | [optional] [example: My Document] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | The Favorites section contents | [**FolderContentIntegerWrapper**](#model-foldercontentintegerwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **403** | You don&#39;t have enough permission to view the folder content | - | - |
| **404** | The required folder was not found | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**FolderContentIntegerWrapper**](#model-foldercontentintegerwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### getFilesUsedSpace

> FilesStatisticsResultWrapper getFilesUsedSpace()

`GET /api/2.0/files/filesusedspace`

Get used space of files

Returns the used space of files in the root folders.

#### Parameters
This endpoint does not need any parameter.

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Used space of files in the root folders | [**FilesStatisticsResultWrapper**](#model-filesstatisticsresultwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**FilesStatisticsResultWrapper**](#model-filesstatisticsresultwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### getFolder

> FormsItemArrayWrapper getFolder(folderId)

`GET /api/2.0/files/{folderId}/formfilter`

Get folder form filter

Returns the form filter of a folder with the ID specified in the request.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **folderId** | path | **Integer** (int32) | The folder unique identifier. | [required] [example: 1] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Ok | [**FormsItemArrayWrapper**](#model-formsitemarraywrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**FormsItemArrayWrapper**](#model-formsitemarraywrapper)

#### Authorization

No authorization required

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### getFolderByFolderId

> FolderContentIntegerWrapper getFolderByFolderId(folderId, userIdOrGroupId, sharedBy, filterType, roomId, folderType, excludeSubject, applyFilterOption, withSubFolders, extension, searchArea, formsItemKey, formsItemType, count, startIndex, sortBy, sortOrder, filterValue, Location)

`GET /api/2.0/files/{folderId}`

Get a folder by ID

Returns the detailed list of files and folders located in the folder with the ID specified in the request.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **folderId** | path | **Integer** (int32) | The folder ID. | [required] [example: 1] |
| **userIdOrGroupId** | query | **UUID** (uuid) | The user or group ID. | [optional] [example: 00000000-0000-0000-0000-000000000000] |
| **sharedBy** | query | **UUID** (uuid) | The identifier of the user who shared the folder or file. | [optional] [example: 00000000-0000-0000-0000-000000000000] |
| **filterType** | query | **FilterType** | The filter type. | [optional] [example: 1] [enum: 0, 1, 2, 3, 4, 5, 7, 8, 9, 10, 11, 12, 13, 14, 17, 20, 22, 23, 24, 25, 26] |
| **roomId** | query | **Integer** (int32) | The room ID. | [optional] [example: 1] |
| **folderType** | query | **List** | The parent folder types used to filter the folder contents by folder type. | [optional] [example: [2]] [enum: 0, 1, 2, 3, 5, 6, 8, 10, 11, 12, 13, 14, 15, 16, 19, 20, 21, 22, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36] |
| **excludeSubject** | query | **Boolean** | Specifies whether to exclude search by user or group ID. | [optional] [example: false] |
| **applyFilterOption** | query | **ApplyFilterOption** | Specifies whether to return only files, only folders, or all elements from the specified folder. | [optional] [example: 1] [enum: 0, 1, 2] |
| **withSubFolders** | query | **Boolean** | Specifies whether to include files from subfolders in the results. | [optional] [example: true] |
| **extension** | query | **String** | Specifies whether to search for the specific file extension. | [optional] [example: .docx] |
| **searchArea** | query | **SearchArea** | The search area. | [optional] [example: 1] [enum: 0, 1, 2, 3, 4, 5, 6, 7, 8, 9] |
| **formsItemKey** | query | **String** | The forms item key. | [optional] [example: doc_key_123] |
| **formsItemType** | query | **String** | The forms item type. | [optional] [example: text] |
| **count** | query | **Integer** (int32) | The maximum number of items to retrieve in the request. | [optional] [example: 25] [min: 1] [max: 100] |
| **startIndex** | query | **Integer** (int32) | The zero-based index of the first item to retrieve in a paginated request. | [optional] [example: 0] |
| **sortBy** | query | **String** | The property used for sorting the folder request results. | [optional] [example: DateAndTime] |
| **sortOrder** | query | **SortOrder** | The order in which the results are sorted. | [optional] [example: 1] [enum: 0, 1] |
| **filterValue** | query | **String** | The text value used as a filter parameter for folder content queries. | [optional] [example: My Document] |
| **Location** | query | **Location** | The location context of the request, specifying the area  where the operation is performed, such as a room, documents, or a link. | [optional] [example: 1] [enum: 1, 2, 3] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Folder contents | [**FolderContentIntegerWrapper**](#model-foldercontentintegerwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **403** | You don&#39;t have enough permission to view the folder content | - | - |
| **404** | The required folder was not found | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**FolderContentIntegerWrapper**](#model-foldercontentintegerwrapper)

#### Authorization

No authorization required

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### getFolderHistory

> HistoryArrayWrapper getFolderHistory(folderId, fromDate, toDate, count, startIndex)

`GET /api/2.0/files/folder/{folderId}/log`

Get folder history

Returns the activity history of a folder with a specified identifier.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **folderId** | path | **Integer** (int32) | The folder ID of the history request. | [required] [example: 1] |
| **fromDate** | query | **ApiDateTime** | The start date of the history request. | [optional] [example: 2025-01-01T00:00:00.0000000Z] |
| **toDate** | query | **ApiDateTime** | The end date of the history request. | [optional] [example: 2025-12-31T23:59:59.0000000Z] |
| **count** | query | **Integer** (int32) | The number of records to retrieve for the folder history. | [optional] [example: 25] [min: 1] [max: 100] |
| **startIndex** | query | **Integer** (int32) | The starting index from which the history records are retrieved in the request. | [optional] [example: 0] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | List of actions in the folder | [**HistoryArrayWrapper**](#model-historyarraywrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **403** | You don&#39;t have enough permission to perform the operation | - | - |
| **404** | The required folder was not found | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**HistoryArrayWrapper**](#model-historyarraywrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### getFolderInfo

> FolderIntegerWrapper getFolderInfo(folderId)

`GET /api/2.0/files/folder/{folderId}`

Get folder information

Returns the detailed information about a folder with the ID specified in the request.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **folderId** | path | **Integer** (int32) | The folder unique identifier. | [required] [example: 1] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Folder parameters | [**FolderIntegerWrapper**](#model-folderintegerwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**FolderIntegerWrapper**](#model-folderintegerwrapper)

#### Authorization

No authorization required

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### getFolderLinks

> FileShareArrayWrapper getFolderLinks(id)

`GET /api/2.0/files/folder/{id}/links`

Get the folder links

Returns the links of the folder with the ID specified in the request.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **id** | path | **Integer** (int32) | The folder ID. | [required] [example: 1] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Folder security information | [**FileShareArrayWrapper**](#model-filesharearraywrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**FileShareArrayWrapper**](#model-filesharearraywrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### getFolderPath

> FileEntryBaseArrayWrapper getFolderPath(folderId)

`GET /api/2.0/files/folder/{folderId}/path`

Get the folder path

Returns a path to the folder with the ID specified in the request.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **folderId** | path | **Integer** (int32) | The folder unique identifier. | [required] [example: 1] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | List of file entry information | [**FileEntryBaseArrayWrapper**](#model-fileentrybasearraywrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **403** | You don&#39;t have enough permission to view the folder content | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**FileEntryBaseArrayWrapper**](#model-fileentrybasearraywrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### getFolderPrimaryExternalLink

> FileShareWrapper getFolderPrimaryExternalLink(id, count, startIndex)

`GET /api/2.0/files/folder/{id}/link`

Get primary external link

Returns the primary external link by the identifier specified in the request.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **id** | path | **Integer** (int32) | The folder unique identifier. | [required] [example: 10] |
| **count** | query | **Integer** (int32) | The number of items to retrieve in the request. | [optional] [example: 25] [min: 1] [max: 100] |
| **startIndex** | query | **Integer** (int32) | The starting index for the query results. | [optional] [example: 0] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Folder security information | [**FileShareWrapper**](#model-filesharewrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **403** | You don&#39;t have enough permission to perform the operation | - | - |
| **404** | Not Found | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**FileShareWrapper**](#model-filesharewrapper)

#### Authorization

No authorization required

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### getFolders

> FileEntryBaseArrayWrapper getFolders(folderId)

`GET /api/2.0/files/{folderId}/subfolders`

Get subfolders

Returns a list of all the subfolders from a folder with the ID specified in the request.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **folderId** | path | **Integer** (int32) | The folder unique identifier. | [required] [example: 1] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | List of file entry information | [**FileEntryBaseArrayWrapper**](#model-fileentrybasearraywrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **403** | You don&#39;t have enough permission to view the folder content | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**FileEntryBaseArrayWrapper**](#model-fileentrybasearraywrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### getFormsFolder

> FolderContentIntegerWrapper getFormsFolder(userIdOrGroupId, filterType, count, startIndex, sortBy, sortOrder, filterValue)

`GET /api/2.0/files/@forms`

Get the Forms section

Returns the detailed list of rooms used for filling out forms located in the Forms section.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **userIdOrGroupId** | query | **UUID** (uuid) | The user or group ID. | [optional] [example: 00000000-0000-0000-0000-000000000000] |
| **filterType** | query | **FilterType** | The filter type. | [optional] [example: 1] [enum: 0, 1, 2, 3, 4, 5, 7, 8, 9, 10, 11, 12, 13, 14, 17, 20, 22, 23, 24, 25, 26] |
| **count** | query | **Integer** (int32) | The maximum number of items to retrieve in the request. | [optional] [example: 25] [min: 1] [max: 100] |
| **startIndex** | query | **Integer** (int32) | The zero-based index of the first item to retrieve in a paginated list. | [optional] [example: 0] |
| **sortBy** | query | **String** | Specifies the field by which the folder content should be sorted. | [optional] [example: DateAndTime] |
| **sortOrder** | query | **SortOrder** | The order in which the results are sorted. | [optional] [example: 1] [enum: 0, 1] |
| **filterValue** | query | **String** | The text used as a filter or search criterion for folder content queries. | [optional] [example: My Document] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | The Forms section contents | [**FolderContentIntegerWrapper**](#model-foldercontentintegerwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **403** | You don&#39;t have enough permission to view the folder content | - | - |
| **404** | The required folder was not found | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**FolderContentIntegerWrapper**](#model-foldercontentintegerwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### getMyFolder

> FolderContentIntegerWrapper getMyFolder(userIdOrGroupId, filterType, applyFilterOption, count, startIndex, sortBy, sortOrder, filterValue)

`GET /api/2.0/files/@my`

Get the My documents section

Returns the detailed list of files and folders located in the My documents section.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **userIdOrGroupId** | query | **UUID** (uuid) | The user or group ID. | [optional] [example: 00000000-0000-0000-0000-000000000000] |
| **filterType** | query | **FilterType** | The filter type. | [optional] [example: 1] [enum: 0, 1, 2, 3, 4, 5, 7, 8, 9, 10, 11, 12, 13, 14, 17, 20, 22, 23, 24, 25, 26] |
| **applyFilterOption** | query | **ApplyFilterOption** | Specifies whether to return only files, only folders or all elements. | [optional] [example: 1] [enum: 0, 1, 2] |
| **count** | query | **Integer** (int32) | The maximum number of items to retrieve in the response. | [optional] [example: 25] [min: 1] [max: 100] |
| **startIndex** | query | **Integer** (int32) | The starting position of the items to be retrieved. | [optional] [example: 0] |
| **sortBy** | query | **String** | The property used to specify the sorting criteria for folder contents. | [optional] [example: DateAndTime] |
| **sortOrder** | query | **SortOrder** | The order in which the results are sorted. | [optional] [example: 1] [enum: 0, 1] |
| **filterValue** | query | **String** | The text used for filtering or searching folder contents. | [optional] [example: My Document] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | The My documents section contents | [**FolderContentIntegerWrapper**](#model-foldercontentintegerwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **403** | You don&#39;t have enough permission to view the folder content | - | - |
| **404** | The required folder was not found | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**FolderContentIntegerWrapper**](#model-foldercontentintegerwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### getNewFolderItems

> FileEntryBaseArrayWrapper getNewFolderItems(folderId)

`GET /api/2.0/files/{folderId}/news`

Get new folder items

Returns a list of all the new items from a folder with the ID specified in the request.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **folderId** | path | **Integer** (int32) | The folder unique identifier. | [required] [example: 1] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | List of file entry information | [**FileEntryBaseArrayWrapper**](#model-fileentrybasearraywrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **403** | You don&#39;t have enough permission to view the folder content | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**FileEntryBaseArrayWrapper**](#model-fileentrybasearraywrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### getRecentFolder

> FolderContentIntegerWrapper getRecentFolder(userIdOrGroupId, filterType, excludeSubject, applyFilterOption, searchArea, extension, count, startIndex, sortBy, sortOrder, filterValue)

`GET /api/2.0/files/recent`

Get the Recent section

Returns the detailed list of files located in the Recent section.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **userIdOrGroupId** | query | **UUID** (uuid) | The user or group ID. | [optional] [example: 00000000-0000-0000-0000-000000000000] |
| **filterType** | query | **FilterType** | The filter type. | [optional] [example: 1] [enum: 0, 1, 2, 3, 4, 5, 7, 8, 9, 10, 11, 12, 13, 14, 17, 20, 22, 23, 24, 25, 26] |
| **excludeSubject** | query | **Boolean** | Specifies whether to exclude search by user or group ID. | [optional] [example: false] |
| **applyFilterOption** | query | **ApplyFilterOption** | Specifies whether to return only files, only folders or all elements. | [optional] [example: 1] [enum: 0, 1, 2] |
| **searchArea** | query | **SearchArea** | The search area. | [optional] [example: 1] [enum: 0, 1, 2, 3, 4, 5, 6, 7, 8, 9] |
| **extension** | query | **List** | Specifies whether to search for a specific file extension in the Recent folder. | [optional] [example: .docx] |
| **count** | query | **Integer** (int32) | The maximum number of items to return. | [optional] [example: 25] [min: 1] [max: 100] |
| **startIndex** | query | **Integer** (int32) | The starting position of the results to be returned in the query response. | [optional] [example: 0] |
| **sortBy** | query | **String** | Specifies the sorting criteria for the folder request. | [optional] [example: DateAndTime] |
| **sortOrder** | query | **SortOrder** | The order in which the results are sorted. | [optional] [example: 1] [enum: 0, 1] |
| **filterValue** | query | **String** | The text used for filtering or searching folder contents. | [optional] [example: My Document] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | The Recent section contents | [**FolderContentIntegerWrapper**](#model-foldercontentintegerwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **403** | You don&#39;t have enough permission to view the folder content | - | - |
| **404** | The required folder was not found | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**FolderContentIntegerWrapper**](#model-foldercontentintegerwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### getReportFolderHistory

> DocumentBuilderTaskWrapper getReportFolderHistory(folderId)

`GET /api/2.0/files/folder/{folderId}/log/report`

Get the folder history report generation status

Returns the status of generating the folder history report.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **folderId** | path | **Integer** (int32) |  | [required] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Operation execution status | [**DocumentBuilderTaskWrapper**](#model-documentbuildertaskwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **403** | You don&#39;t have enough permission to perform the operation | - | - |
| **404** | The required folder was not found | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**DocumentBuilderTaskWrapper**](#model-documentbuildertaskwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### getRootFolders

> FolderContentIntegerArrayWrapper getRootFolders(userIdOrGroupId, filterType, withoutTrash, count, startIndex, sortBy, sortOrder, filterValue)

`GET /api/2.0/files/@root`

Get filtered sections

Returns all the sections matching the parameters specified in the request.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **userIdOrGroupId** | query | **UUID** (uuid) | The user or group ID. | [optional] [example: 00000000-0000-0000-0000-000000000000] |
| **filterType** | query | **FilterType** | The filter type. | [optional] [example: 1] [enum: 0, 1, 2, 3, 4, 5, 7, 8, 9, 10, 11, 12, 13, 14, 17, 20, 22, 23, 24, 25, 26] |
| **withoutTrash** | query | **Boolean** | Specifies whether to return the Trash section or not. | [optional] [example: false] |
| **count** | query | **Integer** (int32) | The maximum number of items to retrieve in the response. | [optional] [example: 25] [min: 1] [max: 100] |
| **startIndex** | query | **Integer** (int32) | The starting position of the items to be retrieved. | [optional] [example: 0] |
| **sortBy** | query | **String** | Specifies the field by which the folder content should be sorted. | [optional] [example: DateAndTime] |
| **sortOrder** | query | **SortOrder** | The order in which the results are sorted. | [optional] [example: 1] [enum: 0, 1] |
| **filterValue** | query | **String** | The text used as a filter for searching or retrieving folder contents. | [optional] [example: My Document] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | List of section contents with the following parameters | [**FolderContentIntegerArrayWrapper**](#model-foldercontentintegerarraywrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **403** | You don&#39;t have enough permission to view the folder content | - | - |
| **404** | The required folder was not found | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**FolderContentIntegerArrayWrapper**](#model-foldercontentintegerarraywrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### getTrashFolder

> FolderContentIntegerWrapper getTrashFolder(userIdOrGroupId, filterType, applyFilterOption, count, startIndex, sortBy, sortOrder, filterValue)

`GET /api/2.0/files/@trash`

Get the Trash section

Returns the detailed list of files and folders located in the Trash section.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **userIdOrGroupId** | query | **UUID** (uuid) | The user or group ID. | [optional] [example: 00000000-0000-0000-0000-000000000000] |
| **filterType** | query | **FilterType** | The filter type. | [optional] [example: 1] [enum: 0, 1, 2, 3, 4, 5, 7, 8, 9, 10, 11, 12, 13, 14, 17, 20, 22, 23, 24, 25, 26] |
| **applyFilterOption** | query | **ApplyFilterOption** | Specifies whether to return only files, only folders or all elements. | [optional] [example: 1] [enum: 0, 1, 2] |
| **count** | query | **Integer** (int32) | The maximum number of items to retrieve in the response. | [optional] [example: 25] [min: 1] [max: 100] |
| **startIndex** | query | **Integer** (int32) | The starting position of the items to be retrieved. | [optional] [example: 0] |
| **sortBy** | query | **String** | The property used to specify the sorting criteria for folder contents. | [optional] [example: DateAndTime] |
| **sortOrder** | query | **SortOrder** | The order in which the results are sorted. | [optional] [example: 1] [enum: 0, 1] |
| **filterValue** | query | **String** | The text used for filtering or searching folder contents. | [optional] [example: My Document] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | The Trash section contents | [**FolderContentIntegerWrapper**](#model-foldercontentintegerwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **403** | You don&#39;t have enough permission to view the folder content | - | - |
| **404** | The required folder was not found | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**FolderContentIntegerWrapper**](#model-foldercontentintegerwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### insertFile

> FileIntegerWrapper insertFile(folderId, InsertFile.File, InsertFile.Title, InsertFile.CreateNewIfExist, InsertFile.KeepConvertStatus, InsertFile.Stream.CanRead, InsertFile.Stream.CanWrite, InsertFile.Stream.CanSeek, InsertFile.Stream.CanTimeout, InsertFile.Stream.Length, InsertFile.Stream.Position, InsertFile.Stream.ReadTimeout, InsertFile.Stream.WriteTimeout)

`POST /api/2.0/files/{folderId}/insert`

Insert a file

Inserts a file specified in the request to the selected folder by single file uploading.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **folderId** | path | **Integer** (int32) | The folder ID for inserting a file. | [required] [example: 1] |
| **InsertFile.File** | form | **File** (binary) | The file to be inserted. | [optional] |
| **InsertFile.Title** | form | **String** | The file title to be inserted. | [optional] |
| **InsertFile.CreateNewIfExist** | form | **Boolean** | Specifies whether to create a new file if it already exists or not. | [optional] |
| **InsertFile.KeepConvertStatus** | form | **Boolean** | Specifies whether to keep the file converting status or not. | [optional] |
| **InsertFile.Stream.CanRead** | form | **Boolean** |  | [optional] |
| **InsertFile.Stream.CanWrite** | form | **Boolean** |  | [optional] |
| **InsertFile.Stream.CanSeek** | form | **Boolean** |  | [optional] |
| **InsertFile.Stream.CanTimeout** | form | **Boolean** |  | [optional] |
| **InsertFile.Stream.Length** | form | **Long** (int64) |  | [optional] |
| **InsertFile.Stream.Position** | form | **Long** (int64) |  | [optional] |
| **InsertFile.Stream.ReadTimeout** | form | **Integer** (int32) |  | [optional] |
| **InsertFile.Stream.WriteTimeout** | form | **Integer** (int32) |  | [optional] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Inserted file | [**FileIntegerWrapper**](#model-fileintegerwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **403** | You don&#39;t have enough permission to create | - | - |
| **404** | Folder not found | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**FileIntegerWrapper**](#model-fileintegerwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: multipart/form-data
- **Accept**: application/json

### insertFileToMyFromBody

> FileIntegerWrapper insertFileToMyFromBody(File, Title, CreateNewIfExist, KeepConvertStatus, Stream.CanRead, Stream.CanWrite, Stream.CanSeek, Stream.CanTimeout, Stream.Length, Stream.Position, Stream.ReadTimeout, Stream.WriteTimeout)

`POST /api/2.0/files/@my/insert`

Insert a file to the My documents section

Inserts a file specified in the request to the My documents section by single file uploading.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **File** | form | **File** (binary) | The file to be inserted. | [optional] |
| **Title** | form | **String** | The file title to be inserted. | [optional] |
| **CreateNewIfExist** | form | **Boolean** | Specifies whether to create a new file if it already exists or not. | [optional] |
| **KeepConvertStatus** | form | **Boolean** | Specifies whether to keep the file converting status or not. | [optional] |
| **Stream.CanRead** | form | **Boolean** |  | [optional] |
| **Stream.CanWrite** | form | **Boolean** |  | [optional] |
| **Stream.CanSeek** | form | **Boolean** |  | [optional] |
| **Stream.CanTimeout** | form | **Boolean** |  | [optional] |
| **Stream.Length** | form | **Long** (int64) |  | [optional] |
| **Stream.Position** | form | **Long** (int64) |  | [optional] |
| **Stream.ReadTimeout** | form | **Integer** (int32) |  | [optional] |
| **Stream.WriteTimeout** | form | **Integer** (int32) |  | [optional] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Inserted file | [**FileIntegerWrapper**](#model-fileintegerwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **403** | You don&#39;t have enough permission to create | - | - |
| **404** | Folder not found | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**FileIntegerWrapper**](#model-fileintegerwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: multipart/form-data
- **Accept**: application/json

### renameFolder

> FolderIntegerWrapper renameFolder(folderId, CreateFolder)

`PUT /api/2.0/files/folder/{folderId}`

Rename a folder

Renames the selected folder with a new title specified in the request.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **folderId** | path | **Integer** (int32) | The folder ID for the folder creation. | [required] [example: 1] |
| **CreateFolder** | body | [**CreateFolder**](#model-createfolder) | The parameters for creating a folder. | [required] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Folder parameters | [**FolderIntegerWrapper**](#model-folderintegerwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **403** | You don&#39;t have enough permission to rename the folder | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**FolderIntegerWrapper**](#model-folderintegerwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

### setFolderOrder

> FolderIntegerWrapper setFolderOrder(folderId, OrderRequestDto)

`PUT /api/2.0/files/folder/{folderId}/order`

Set folder order

Sets the order of a folder with ID specified in the request.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **folderId** | path | **Integer** (int32) | The folder unique identifier. | [required] [example: 1] |
| **OrderRequestDto** | body | [**OrderRequestDto**](#model-orderrequestdto) | The folder order information. | [optional] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | List of file operations | [**FolderIntegerWrapper**](#model-folderintegerwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**FolderIntegerWrapper**](#model-folderintegerwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

### setFolderPrimaryExternalLink

> FileShareWrapper setFolderPrimaryExternalLink(id, FolderLinkRequest)

`PUT /api/2.0/files/folder/{id}/links`

Set the folder external link

Sets the folder external link with the ID specified in the request.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **id** | path | **Integer** (int32) | The folder ID. | [required] [example: 1] |
| **FolderLinkRequest** | body | [**FolderLinkRequest**](#model-folderlinkrequest) | The folder link parameters. | [required] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Folder information | [**FileShareWrapper**](#model-filesharewrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**FileShareWrapper**](#model-filesharewrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

### terminateReportFolderHistory

> terminateReportFolderHistory(folderId)

`DELETE /api/2.0/files/folder/{folderId}/log/report`

Terminate the folder history report generation

Terminates generating the folder history report.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **folderId** | path | **Integer** (int32) |  | [required] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Ok | - | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **403** | You don&#39;t have enough permission to perform the operation | - | - |
| **404** | The required folder was not found | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

null (empty response body)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: Not defined

### uploadFile

> FileIntegerArrayWrapper uploadFile(folderId, createNewIfExist, storeOriginalFile, keepConvertStatus, File)

`POST /api/2.0/files/{folderId}/upload`

Upload a file

Uploads a file specified in the request to the selected folder by single file uploading or standart multipart/form-data method.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **folderId** | path | **Integer** (int32) | The folder ID to upload a file. | [required] [example: 1] |
| **createNewIfExist** | query | **Boolean** | Specifies whether to create the new file if it already exists or not. | [optional] [example: true] |
| **storeOriginalFile** | query | **Boolean** | Specifies whether to upload documents in the original formats as well or not. | [optional] [example: true] |
| **keepConvertStatus** | query | **Boolean** | Specifies whether to keep the file converting status or not. | [optional] [example: false] |
| **File** | form | **File** (binary) | The file to be uploaded. | [optional] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Inserted file | [**FileIntegerArrayWrapper**](#model-fileintegerarraywrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **403** | You don&#39;t have enough permission to create | - | - |
| **404** | Folder not found | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**FileIntegerArrayWrapper**](#model-fileintegerarraywrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: multipart/form-data
- **Accept**: application/json

### uploadFileToMy

> FileIntegerArrayWrapper uploadFileToMy(createNewIfExist, storeOriginalFile, keepConvertStatus, File)

`POST /api/2.0/files/@my/upload`

Upload a file to the My documents section

Uploads a file specified in the request to the My documents section by single file uploading or standart multipart/form-data method.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **createNewIfExist** | query | **Boolean** | Specifies whether to create the new file if it already exists or not. | [optional] [example: true] |
| **storeOriginalFile** | query | **Boolean** | Specifies whether to upload documents in the original formats as well or not. | [optional] [example: true] |
| **keepConvertStatus** | query | **Boolean** | Specifies whether to keep the file converting status or not. | [optional] [example: false] |
| **File** | form | **File** (binary) | The file to be uploaded. | [optional] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Uploaded file(s) | [**FileIntegerArrayWrapper**](#model-fileintegerarraywrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **403** | You don&#39;t have enough permission to create | - | - |
| **404** | File not found | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**FileIntegerArrayWrapper**](#model-fileintegerarraywrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: multipart/form-data
- **Accept**: application/json

## FilesOperationsApi

### abortUploadSession

> abortUploadSession(sessionId, folderId)

`DELETE /api/2.0/files/{folderId}/session/{sessionId}`

Aborts an in-progress file upload session.

This method allows users to cancel an ongoing upload session identified by the session ID.  Once the session is aborted, the associated resources will be cleaned up, and the session will no longer accept further uploads.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **sessionId** | path | **String** | The session ID. | [required] [example: session-123-abc] |
| **folderId** | path | **Integer** (int32) | The folder ID. | [required] [example: 1] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | OK | - | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

null (empty response body)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: Not defined

### addFavorites

> BooleanWrapper addFavorites(BaseBatchRequestDto)

`POST /api/2.0/files/favorites`

Add favorite files and folders

Adds files and folders with the IDs specified in the request to the favorite list.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **BaseBatchRequestDto** | body | [**BaseBatchRequestDto**](#model-basebatchrequestdto) |  | [optional] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Boolean value: true if the operation is successful | [**BooleanWrapper**](#model-booleanwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **403** | You don&#39;t have enough permission to perform the operation | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**BooleanWrapper**](#model-booleanwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

### bulkDownload

> FileOperationArrayWrapper bulkDownload(DownloadRequestDto)

`PUT /api/2.0/files/fileops/bulkdownload`

Bulk download

Starts the download process of files and folders with the IDs specified in the request.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **DownloadRequestDto** | body | [**DownloadRequestDto**](#model-downloadrequestdto) |  | [optional] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | List of file operations | [**FileOperationArrayWrapper**](#model-fileoperationarraywrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **403** | You don&#39;t have enough permission to download | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**FileOperationArrayWrapper**](#model-fileoperationarraywrapper)

#### Authorization

No authorization required

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

### checkConversionStatus

> ConversationResultArrayWrapper checkConversionStatus(fileId, start)

`GET /api/2.0/files/file/{fileId}/checkconversion`

Get conversion status

Checks the conversion status of a file with the ID specified in the request.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **fileId** | path | **Integer** (int32) | The file ID to check conversion status. | [required] [example: 1] |
| **start** | query | **Boolean** | Specifies whether a conversion operation is started or not. | [optional] [example: false] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Conversion result | [**ConversationResultArrayWrapper**](#model-conversationresultarraywrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**ConversationResultArrayWrapper**](#model-conversationresultarraywrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### checkMoveOrCopyBatchItems

> FileEntryBaseArrayWrapper checkMoveOrCopyBatchItems(inDto)

`GET /api/2.0/files/fileops/move`

Move or copy files to a folder

Checks if files or folders can be moved or copied to the specified folder, moves or copies them, and returns their information.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **inDto** | query | **BatchRequestDto** | The request parameters for copying/moving files. | [optional] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | List of file entry information | [**FileEntryBaseArrayWrapper**](#model-fileentrybasearraywrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **403** | You don&#39;t have enough permission to create | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**FileEntryBaseArrayWrapper**](#model-fileentrybasearraywrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### checkMoveOrCopyDestFolder

> CheckDestFolderWrapper checkMoveOrCopyDestFolder(inDto)

`GET /api/2.0/files/fileops/checkdestfolder`

Check for moving or copying files to a folder

Checks if files can be moved or copied to the specified folder.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **inDto** | query | **BatchRequestDto** | The request parameters for copying/moving files. | [optional] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Result | [**CheckDestFolderWrapper**](#model-checkdestfolderwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **403** | You don&#39;t have enough permission to create | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**CheckDestFolderWrapper**](#model-checkdestfolderwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### copyBatchItems

> FileOperationArrayWrapper copyBatchItems(BatchRequestDto)

`PUT /api/2.0/files/fileops/copy`

Copy to the folder

Copies all the selected files and folders to the folder with the ID specified in the request.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **BatchRequestDto** | body | [**BatchRequestDto**](#model-batchrequestdto) |  | [optional] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | List of file operations | [**FileOperationArrayWrapper**](#model-fileoperationarraywrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **403** | You don&#39;t have enough permission to copy | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**FileOperationArrayWrapper**](#model-fileoperationarraywrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

### createUploadSession

> ChunkedUploadSessionResponseWrapperIntegerWrapper createUploadSession(folderId, SessionRequest)

`POST /api/2.0/files/{folderId}/upload/create_session`

Chunked upload

Creates the session to upload large files in multiple chunks to the folder with the ID specified in the request.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **folderId** | path | **Integer** (int32) | The session folder ID. | [required] [example: 1] |
| **SessionRequest** | body | [**SessionRequest**](#model-sessionrequest) | The session parameters. | [required] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Information about created session | [**ChunkedUploadSessionResponseWrapperIntegerWrapper**](#model-chunkeduploadsessionresponsewrapperintegerwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **403** | You don&#39;t have enough permission to create | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**ChunkedUploadSessionResponseWrapperIntegerWrapper**](#model-chunkeduploadsessionresponsewrapperintegerwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

### createUploadSessionInFolder

> ChunkedUploadSessionResponseIntegerWrapper createUploadSessionInFolder(folderId, SessionRequest)

`POST /api/2.0/files/{folderId}/session`

Creates a session for uploading a file to a specific folder in chunks.

The session allows the user to upload a file in smaller chunks to the folder identified by its ID.  The file information, such as name, size, and additional metadata, must be provided in the request.  This method facilitates large file upload scenarios by enabling chunked file uploads.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **folderId** | path | **Integer** (int32) | The session folder ID. | [required] [example: 1] |
| **SessionRequest** | body | [**SessionRequest**](#model-sessionrequest) | The session parameters. | [required] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | OK | [**ChunkedUploadSessionResponseIntegerWrapper**](#model-chunkeduploadsessionresponseintegerwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**ChunkedUploadSessionResponseIntegerWrapper**](#model-chunkeduploadsessionresponseintegerwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

### deleteBatchItems

> FileOperationArrayWrapper deleteBatchItems(DeleteBatchRequestDto)

`PUT /api/2.0/files/fileops/delete`

Delete files and folders

Deletes the files and folders with the IDs specified in the request.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **DeleteBatchRequestDto** | body | [**DeleteBatchRequestDto**](#model-deletebatchrequestdto) |  | [optional] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | List of file operations | [**FileOperationArrayWrapper**](#model-fileoperationarraywrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **403** | You don&#39;t have enough permission to delete | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**FileOperationArrayWrapper**](#model-fileoperationarraywrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

### deleteFavoritesFromBody

> BooleanWrapper deleteFavoritesFromBody(BaseBatchRequestDto)

`DELETE /api/2.0/files/favorites`

Delete favorite files and folders (using body parameters)

Removes files and folders with the IDs specified in the request from the favorite list. This method uses the body parameters.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **BaseBatchRequestDto** | body | [**BaseBatchRequestDto**](#model-basebatchrequestdto) |  | [optional] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Boolean value: true if the operation is successful | [**BooleanWrapper**](#model-booleanwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**BooleanWrapper**](#model-booleanwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

### deleteFileVersions

> FileOperationWrapper deleteFileVersions(DeleteVersionBatchRequestDto)

`PUT /api/2.0/files/fileops/deleteversion`

Delete file versions

Deletes the file versions with the IDs specified in the request.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **DeleteVersionBatchRequestDto** | body | [**DeleteVersionBatchRequestDto**](#model-deleteversionbatchrequestdto) |  | [optional] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | List of file operations | [**FileOperationWrapper**](#model-fileoperationwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**FileOperationWrapper**](#model-fileoperationwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

### duplicateBatchItems

> FileOperationArrayWrapper duplicateBatchItems(DuplicateRequestDto)

`PUT /api/2.0/files/fileops/duplicate`

Duplicate files and folders

Duplicates all the selected files and folders.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **DuplicateRequestDto** | body | [**DuplicateRequestDto**](#model-duplicaterequestdto) |  | [optional] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | List of file operations | [**FileOperationArrayWrapper**](#model-fileoperationarraywrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **403** | You don&#39;t have enough permission to duplicate | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**FileOperationArrayWrapper**](#model-fileoperationarraywrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

### emptyTrash

> FileOperationArrayWrapper emptyTrash(Single)

`PUT /api/2.0/files/fileops/emptytrash`

Empty the Trash folder

Deletes all the files and folders from the Trash folder.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **Single** | query | **Boolean** | Specifies whether to return only the current operation | [optional] [example: false] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | List of file operations | [**FileOperationArrayWrapper**](#model-fileoperationarraywrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**FileOperationArrayWrapper**](#model-fileoperationarraywrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### finalizeSession

> UploadSessionResponseIntegerWrapper finalizeSession(folderId, sessionId)

`PUT /api/2.0/files/{folderId}/session/{sessionId}/finalize`

Finalize an upload session

Finalizes the upload session by processing the uploaded file chunks and marking the upload as complete.  This method consolidates chunked uploads into a complete file if required, sends notifications about the upload event,  and performs any additional cleanup or related actions, such as socket updates and webhook publishing.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **folderId** | path | **Integer** (int32) | The folder ID. | [required] [example: 1] |
| **sessionId** | path | **String** | The session ID. | [required] [example: doc_key_123] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | OK | [**UploadSessionResponseIntegerWrapper**](#model-uploadsessionresponseintegerwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**UploadSessionResponseIntegerWrapper**](#model-uploadsessionresponseintegerwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### getOperationStatuses

> FileOperationArrayWrapper getOperationStatuses(id)

`GET /api/2.0/files/fileops`

Get active file operations

Returns a list of all the active file operations.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **id** | query | **String** | The ID of the file operation. | [optional] [example: operation-123-abc] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | List of file operations | [**FileOperationArrayWrapper**](#model-fileoperationarraywrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**FileOperationArrayWrapper**](#model-fileoperationarraywrapper)

#### Authorization

No authorization required

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### getOperationStatusesByType

> FileOperationArrayWrapper getOperationStatusesByType(operationType, id)

`GET /api/2.0/files/fileops/{operationType}`

Get file operation statuses

Retrieves the statuses of operations filtered by the specified operation type.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **operationType** | path | **FileOperationType** | Specifies the type of file operation to be retrieved. | [required] [example: 0] [enum: 0, 1, 2, 3, 4, 5, 6, 7] |
| **id** | query | **String** | The ID of the file operation. | [optional] [example: operation-123-abc] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | List of file operations | [**FileOperationArrayWrapper**](#model-fileoperationarraywrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**FileOperationArrayWrapper**](#model-fileoperationarraywrapper)

#### Authorization

No authorization required

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### markAsRead

> FileOperationArrayWrapper markAsRead(BaseBatchRequestDto)

`PUT /api/2.0/files/fileops/markasread`

Mark as read

Marks the files and folders with the IDs specified in the request as read.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **BaseBatchRequestDto** | body | [**BaseBatchRequestDto**](#model-basebatchrequestdto) |  | [optional] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | List of file operations | [**FileOperationArrayWrapper**](#model-fileoperationarraywrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**FileOperationArrayWrapper**](#model-fileoperationarraywrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

### moveBatchItems

> FileOperationArrayWrapper moveBatchItems(BatchRequestDto)

`PUT /api/2.0/files/fileops/move`

Move or copy to a folder

Moves or copies all the selected files and folders to the folder with the ID specified in the request.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **BatchRequestDto** | body | [**BatchRequestDto**](#model-batchrequestdto) |  | [optional] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | List of file operations | [**FileOperationArrayWrapper**](#model-fileoperationarraywrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **403** | You don&#39;t have enough permission to move | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**FileOperationArrayWrapper**](#model-fileoperationarraywrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

### startFileConversion

> ConversationResultArrayWrapper startFileConversion(fileId, CheckConversionRequestDtoInteger)

`PUT /api/2.0/files/file/{fileId}/checkconversion`

Start file conversion

Starts a conversion operation of a file with the ID specified in the request.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **fileId** | path | **Integer** (int32) | The file ID to start conversion proccess. | [required] [example: 1] |
| **CheckConversionRequestDtoInteger** | body | [**CheckConversionRequestDtoInteger**](#model-checkconversionrequestdtointeger) | The parameters for checking file conversion. | [optional] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Conversion result | [**ConversationResultArrayWrapper**](#model-conversationresultarraywrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**ConversationResultArrayWrapper**](#model-conversationresultarraywrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

### terminateTasks

> FileOperationArrayWrapper terminateTasks(id)

`PUT /api/2.0/files/fileops/terminate/{id}`

Finish active operations

Finishes an operation with the ID specified in the request or all the active operations.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **id** | path | **String** | The operation unique identifier. | [required] [example: some-operation-id] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | List of file operations | [**FileOperationArrayWrapper**](#model-fileoperationarraywrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**FileOperationArrayWrapper**](#model-fileoperationarraywrapper)

#### Authorization

No authorization required

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### updateFileComment

> StringWrapper updateFileComment(fileId, UpdateComment)

`PUT /api/2.0/files/file/{fileId}/comment`

Update a comment

Updates a comment in a file with the ID specified in the request.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **fileId** | path | **Integer** (int32) | The file ID where the comment is located. | [required] [example: 1] |
| **UpdateComment** | body | [**UpdateComment**](#model-updatecomment) | The parameters for updating a comment. | [required] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Updated comment | [**StringWrapper**](#model-stringwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**StringWrapper**](#model-stringwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

### uploadAsyncSession

> ChunkedUploadSessionResponseIntegerWrapper uploadAsyncSession(folderId, sessionId, ChunkNumber, File)

`POST /api/2.0/files/{folderId}/session/{sessionId}/upload`

Handles the upload of a chunk for an existing upload session.

This method allows the caller to upload a specific chunk of a file to an ongoing upload session.  The session is identified by the session ID provided in the request. The chunk can be of any size  within the limits allowed during the session initialization. Each chunk must be uploaded in the  correct order for the server to process it appropriately.  The server updates the upload session status and stores the progress information after processing  each chunk. The updated session details are returned in the response.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **folderId** | path | **Integer** (int32) | The folder ID. | [required] [example: 1] |
| **sessionId** | path | **String** | The upload session ID. | [required] [example: session_abc123] |
| **ChunkNumber** | query | **Integer** (int32) | The chunk number. | [optional] [example: 1] |
| **File** | form | **File** (binary) | The file chunk to be uploaded as part of the multipart/form-data request.  This property represents the uploaded file chunk content from the HTTP request form for chunked upload operations.  The file chunk is accessed via the IFormFile interface which provides access to the chunk content and length. | [optional] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | OK | [**ChunkedUploadSessionResponseIntegerWrapper**](#model-chunkeduploadsessionresponseintegerwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**ChunkedUploadSessionResponseIntegerWrapper**](#model-chunkeduploadsessionresponseintegerwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: multipart/form-data
- **Accept**: application/json

### uploadSession

> UploadSessionResponseIntegerWrapper uploadSession(folderId, sessionId, File)

`POST /api/2.0/files/{folderId}/session/{sessionId}`

Resumes an ongoing file upload session for uploading additional chunks of data.

This method allows continuing an interrupted or partially completed file upload session by uploading subsequent data chunks.  The server will validate each uploaded chunk, update the session state, and respond with the status of the current upload. Once  the total bytes uploaded match the total file size, the file upload process is finalized and related events are triggered.  If the file is newly uploaded, the server responds with a 201 Created status upon completion. If it overwrites an existing file,  versioning information is updated accordingly. The method also triggers associated webhooks and socket notifications to reflect  the updated file state.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **folderId** | path | **Integer** (int32) | The folder ID. | [required] [example: 1] |
| **sessionId** | path | **String** | The upload session ID. | [required] [example: session_abc123] |
| **File** | form | **File** (binary) | The file to be uploaded as part of the multipart/form-data request.  This property represents the uploaded file content from the HTTP request form.  The file is accessed via the IFormFile interface which provides access to the file name, content type, length, and stream. | [optional] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | OK | [**UploadSessionResponseIntegerWrapper**](#model-uploadsessionresponseintegerwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**UploadSessionResponseIntegerWrapper**](#model-uploadsessionresponseintegerwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: multipart/form-data
- **Accept**: application/json

## FilesQuotaApi

### resetRoomQuota

> FolderIntegerArrayWrapper resetRoomQuota(UpdateRoomsRoomIdsRequestDtoInteger)

`PUT /api/2.0/files/rooms/resetquota`

Reset the room quota limit

Resets the quota limit for the rooms with the IDs specified in the request.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **UpdateRoomsRoomIdsRequestDtoInteger** | body | [**UpdateRoomsRoomIdsRequestDtoInteger**](#model-updateroomsroomidsrequestdtointeger) |  | [optional] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | List of rooms with the detailed information | [**FolderIntegerArrayWrapper**](#model-folderintegerarraywrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**FolderIntegerArrayWrapper**](#model-folderintegerarraywrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

### updateRoomsQuota

> FolderIntegerArrayWrapper updateRoomsQuota(UpdateRoomsQuotaRequestDtoInteger)

`PUT /api/2.0/files/rooms/roomquota`

Change the room quota limit

Changes the quota limit for the rooms with the IDs specified in the request.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **UpdateRoomsQuotaRequestDtoInteger** | body | [**UpdateRoomsQuotaRequestDtoInteger**](#model-updateroomsquotarequestdtointeger) |  | [optional] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | List of rooms with the detailed information | [**FolderIntegerArrayWrapper**](#model-folderintegerarraywrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**FolderIntegerArrayWrapper**](#model-folderintegerarraywrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

## FilesSettingsApi

### changeAccessToThirdparty

> BooleanWrapper changeAccessToThirdparty(SettingsRequestDto)

`PUT /api/2.0/files/thirdparty`

Change the third-party settings access

Changes the access to the third-party settings.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **SettingsRequestDto** | body | [**SettingsRequestDto**](#model-settingsrequestdto) |  | [optional] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Boolean value: true if the operation is successful | [**BooleanWrapper**](#model-booleanwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**BooleanWrapper**](#model-booleanwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

### changeAutomaticallyCleanUp

> AutoCleanUpDataWrapper changeAutomaticallyCleanUp(AutoCleanupRequestDto)

`PUT /api/2.0/files/settings/autocleanup`

Update the trash bin auto-clearing setting

Updates the trash bin auto-clearing setting.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **AutoCleanupRequestDto** | body | [**AutoCleanupRequestDto**](#model-autocleanuprequestdto) |  | [optional] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | The auto-clearing setting properties: auto-clearing or not, a time interval when the auto-clearing will be performed | [**AutoCleanUpDataWrapper**](#model-autocleanupdatawrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**AutoCleanUpDataWrapper**](#model-autocleanupdatawrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

### changeDefaultAccessRights

> FileShareArrayWrapper changeDefaultAccessRights(request\_body)

`PUT /api/2.0/files/settings/dafaultaccessrights`

Change the default access rights

Changes the default access rights in the sharing settings.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **request\_body** | body | **List** | Sharing rights (None, ReadWrite, Read, Restrict, Varies, Review, Comment, FillForms, CustomFilter, RoomAdmin, Editing, Collaborator). | [optional] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Updated sharing rights (None, ReadWrite, Read, Restrict, Varies, Review, Comment, FillForms, CustomFilter, RoomAdmin, Editing, Collaborator) | [**FileShareArrayWrapper**](#model-filesharearraywrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**FileShareArrayWrapper**](#model-filesharearraywrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

### changeDeleteConfirm

> BooleanWrapper changeDeleteConfirm(SettingsRequestDto)

`PUT /api/2.0/files/changedeleteconfrim`

Confirm the file deletion

Specifies whether to confirm the file deletion or not.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **SettingsRequestDto** | body | [**SettingsRequestDto**](#model-settingsrequestdto) |  | [optional] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Boolean value: true if the operation is successful | [**BooleanWrapper**](#model-booleanwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**BooleanWrapper**](#model-booleanwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

### changeDownloadZip

> ICompressWrapper changeDownloadZip(DisplayRequestDto)

`PUT /api/2.0/files/settings/downloadtargz`

Change the archive format (using body parameters)

Changes the format of the downloaded archive from .zip to .tar.gz. This method uses the body parameters.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **DisplayRequestDto** | body | [**DisplayRequestDto**](#model-displayrequestdto) |  | [optional] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Archive | [**ICompressWrapper**](#model-icompresswrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**ICompressWrapper**](#model-icompresswrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

### changeExternalSharingSettings

> ExternalSharingSettingsWrapper changeExternalSharingSettings(ExternalSharingSettingsRequestDto)

`PUT /api/2.0/files/settings/externalsharingsettings`

Change the Access Control external sharing settings

Changes the Access Control external sharing settings.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **ExternalSharingSettingsRequestDto** | body | [**ExternalSharingSettingsRequestDto**](#model-externalsharingsettingsrequestdto) |  | [optional] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | External sharing settings | [**ExternalSharingSettingsWrapper**](#model-externalsharingsettingswrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**ExternalSharingSettingsWrapper**](#model-externalsharingsettingswrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

### checkDocServiceUrl

> DocServiceUrlWrapper checkDocServiceUrl(CheckDocServiceUrlRequestDto)

`PUT /api/2.0/files/docservice`

Check the document service URL

Checks the document service location URL.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **CheckDocServiceUrlRequestDto** | body | [**CheckDocServiceUrlRequestDto**](#model-checkdocserviceurlrequestdto) |  | [optional] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Document service information: the Document Server address, the Document Server address in the local private network, the Community Server address | [**DocServiceUrlWrapper**](#model-docserviceurlwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **400** | Invalid input urls/Mixed Active Content is not allowed. HTTPS address for Document Server is required | - | - |
| **403** | You don&#39;t have enough permission to perform the operation | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**DocServiceUrlWrapper**](#model-docserviceurlwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

### displayFileExtension

> BooleanWrapper displayFileExtension(SettingsRequestDto)

`PUT /api/2.0/files/displayfileextension`

Display a file extension

Specifies whether to display a file extension or not.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **SettingsRequestDto** | body | [**SettingsRequestDto**](#model-settingsrequestdto) |  | [optional] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Boolean value: true if the parameter is enabled | [**BooleanWrapper**](#model-booleanwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**BooleanWrapper**](#model-booleanwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

### displayRecent

> BooleanWrapper displayRecent(DisplayRequestDto)

`PUT /api/2.0/files/displayrecent`

Display the Recent folder

Displays the Recent folder.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **DisplayRequestDto** | body | [**DisplayRequestDto**](#model-displayrequestdto) |  | [optional] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Boolean value: true if the parameter is enabled | [**BooleanWrapper**](#model-booleanwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **403** | You don&#39;t have enough permission to perform the operation | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**BooleanWrapper**](#model-booleanwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

### externalShare

> BooleanWrapper externalShare(DisplayRequestDto)

`PUT /api/2.0/files/settings/external`

Change the external sharing ability

Changes the ability to share a file externally.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **DisplayRequestDto** | body | [**DisplayRequestDto**](#model-displayrequestdto) |  | [optional] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Boolean value: true if the parameter is enabled | [**BooleanWrapper**](#model-booleanwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**BooleanWrapper**](#model-booleanwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

### externalShareSocialMedia

> BooleanWrapper externalShareSocialMedia(DisplayRequestDto)

`PUT /api/2.0/files/settings/externalsocialmedia`

Change the external sharing ability on social networks

Changes the ability to share a file externally on social networks.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **DisplayRequestDto** | body | [**DisplayRequestDto**](#model-displayrequestdto) |  | [optional] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Boolean value: true if the parameter is enabled | [**BooleanWrapper**](#model-booleanwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**BooleanWrapper**](#model-booleanwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

### forcesave

> BooleanWrapper forcesave()

`PUT /api/2.0/files/forcesave`

Change the forcesaving ability

Specifies if the file forcesaving is enabled or not.

#### Parameters
This endpoint does not need any parameter.

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Boolean value: true if the parameter is enabled | [**BooleanWrapper**](#model-booleanwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**BooleanWrapper**](#model-booleanwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### getAutomaticallyCleanUp

> AutoCleanUpDataWrapper getAutomaticallyCleanUp()

`GET /api/2.0/files/settings/autocleanup`

Get the trash bin auto-clearing setting

Returns the trash bin auto-clearing setting.

#### Parameters
This endpoint does not need any parameter.

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | The auto-clearing setting properties: auto-clearing or not, a time interval when the auto-clearing will be performed | [**AutoCleanUpDataWrapper**](#model-autocleanupdatawrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**AutoCleanUpDataWrapper**](#model-autocleanupdatawrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### getDefaultTemplates

> DefaultTemplateSettingsWrapper getDefaultTemplates()

`GET /api/2.0/files/settings/defaulttemplate`

Get the default template setting

Returns the default template setting.

#### Parameters
This endpoint does not need any parameter.

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Default template settings | [**DefaultTemplateSettingsWrapper**](#model-defaulttemplatesettingswrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **403** | You don&#39;t have enough permission to perform the operation | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**DefaultTemplateSettingsWrapper**](#model-defaulttemplatesettingswrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### getDocServiceUrl

> DocServiceUrlWrapper getDocServiceUrl(version)

`GET /api/2.0/files/docservice`

Get the document service URL

Returns the URL address of the connected editors.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **version** | query | **Boolean** | Specifies whether to return the editor version or not. | [optional] [example: true] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | The document service URL with the editor version specified | [**DocServiceUrlWrapper**](#model-docserviceurlwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**DocServiceUrlWrapper**](#model-docserviceurlwrapper)

#### Authorization

No authorization required

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### getFilesModule

> ModuleWrapper getFilesModule()

`GET /api/2.0/files/info`

Get the Documents information

Returns the information about the Documents module.

#### Parameters
This endpoint does not need any parameter.

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Module information: ID, product class name, title, description, icon URL, large icon URL, start URL, primary or nor, help URL | [**ModuleWrapper**](#model-modulewrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**ModuleWrapper**](#model-modulewrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### getFilesSettings

> FilesSettingsWrapper getFilesSettings()

`GET /api/2.0/files/settings`

Get file settings

Returns all the file settings.

#### Parameters
This endpoint does not need any parameter.

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | File settings | [**FilesSettingsWrapper**](#model-filessettingswrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**FilesSettingsWrapper**](#model-filessettingswrapper)

#### Authorization

No authorization required

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### hideConfirmCancelOperation

> BooleanWrapper hideConfirmCancelOperation(SettingsRequestDto)

`PUT /api/2.0/files/hideconfirmcanceloperation`

Hide confirmation dialog when canceling operations

Hides the confirmation dialog when canceling operations.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **SettingsRequestDto** | body | [**SettingsRequestDto**](#model-settingsrequestdto) |  | [optional] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Boolean value: true if the parameter is enabled | [**BooleanWrapper**](#model-booleanwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**BooleanWrapper**](#model-booleanwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

### hideConfirmConvert

> BooleanWrapper hideConfirmConvert(HideConfirmConvertRequestDto)

`PUT /api/2.0/files/hideconfirmconvert`

Hide the confirmation dialog when converting

Hides the confirmation dialog for saving the file copy in the original format when converting a file.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **HideConfirmConvertRequestDto** | body | [**HideConfirmConvertRequestDto**](#model-hideconfirmconvertrequestdto) |  | [optional] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Boolean value: true if the operation is successful | [**BooleanWrapper**](#model-booleanwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**BooleanWrapper**](#model-booleanwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

### hideConfirmRoomLifetime

> BooleanWrapper hideConfirmRoomLifetime(SettingsRequestDto)

`PUT /api/2.0/files/hideconfirmroomlifetime`

Hide confirmation dialog when changing room lifetime settings

Hides the confirmation dialog when changing the room lifetime settings.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **SettingsRequestDto** | body | [**SettingsRequestDto**](#model-settingsrequestdto) |  | [optional] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Boolean value: true if the parameter is enabled | [**BooleanWrapper**](#model-booleanwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**BooleanWrapper**](#model-booleanwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

### keepNewFileName

> BooleanWrapper keepNewFileName(SettingsRequestDto)

`PUT /api/2.0/files/keepnewfilename`

Ask a new file name

Specifies whether to ask a user for a file name on creation or not.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **SettingsRequestDto** | body | [**SettingsRequestDto**](#model-settingsrequestdto) |  | [optional] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Boolean value: true if the operation is successful | [**BooleanWrapper**](#model-booleanwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**BooleanWrapper**](#model-booleanwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

### resetDefaultTemplate

> DefaultTemplateSettingsWrapper resetDefaultTemplate(DefaultTemplateSettingsResetRequestDto)

`DELETE /api/2.0/files/settings/defaulttemplate`

Reset the default template setting

Resets the default template setting.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **DefaultTemplateSettingsResetRequestDto** | body | [**DefaultTemplateSettingsResetRequestDto**](#model-defaulttemplatesettingsresetrequestdto) |  | [optional] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | New default template settings | [**DefaultTemplateSettingsWrapper**](#model-defaulttemplatesettingswrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **403** | You don&#39;t have enough permission to perform the operation | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**DefaultTemplateSettingsWrapper**](#model-defaulttemplatesettingswrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

### setDefaultTemplate

> DefaultTemplateSettingsWrapper setDefaultTemplate(DefaultTemplateSettingsRequestDto)

`PUT /api/2.0/files/settings/defaulttemplate`

Change the default template setting

Changes the default template setting.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **DefaultTemplateSettingsRequestDto** | body | [**DefaultTemplateSettingsRequestDto**](#model-defaulttemplatesettingsrequestdto) |  | [optional] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | New default template settings | [**DefaultTemplateSettingsWrapper**](#model-defaulttemplatesettingswrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **400** | Incorrect or missing file | - | - |
| **403** | You don&#39;t have enough permission to perform the operation | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**DefaultTemplateSettingsWrapper**](#model-defaulttemplatesettingswrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

### setOpenEditorInSameTab

> BooleanWrapper setOpenEditorInSameTab(SettingsRequestDto)

`PUT /api/2.0/files/settings/openeditorinsametab`

Open document in the same browser tab

Changes the ability to open the document in the same browser tab.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **SettingsRequestDto** | body | [**SettingsRequestDto**](#model-settingsrequestdto) |  | [optional] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Boolean value: true if the parameter is enabled | [**BooleanWrapper**](#model-booleanwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**BooleanWrapper**](#model-booleanwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

### setOrganizeRoomsGrouping

> BooleanWrapper setOrganizeRoomsGrouping(SettingsRequestDto)

`PUT /api/2.0/files/settings/organizegrouping`

Organize rooms grouping

Changes the setting that allows the user to organize the grouping of rooms.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **SettingsRequestDto** | body | [**SettingsRequestDto**](#model-settingsrequestdto) |  | [optional] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Boolean value: true if the parameter is enabled | [**BooleanWrapper**](#model-booleanwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**BooleanWrapper**](#model-booleanwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

### storeForcesave

> BooleanWrapper storeForcesave()

`PUT /api/2.0/files/storeforcesave`

Change the ability to store the forcesaved files

Changes the ability to store the forcesaved file versions.

#### Parameters
This endpoint does not need any parameter.

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Boolean value: true if the operation is successful | [**BooleanWrapper**](#model-booleanwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**BooleanWrapper**](#model-booleanwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### storeOriginal

> BooleanWrapper storeOriginal(SettingsRequestDto)

`PUT /api/2.0/files/storeoriginal`

Change the ability to upload original formats

Changes the ability to upload documents in the original formats as well.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **SettingsRequestDto** | body | [**SettingsRequestDto**](#model-settingsrequestdto) |  | [optional] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Boolean value: true if the operation is successful | [**BooleanWrapper**](#model-booleanwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**BooleanWrapper**](#model-booleanwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

### updateFileIfExist

> BooleanWrapper updateFileIfExist(SettingsRequestDto)

`PUT /api/2.0/files/updateifexist`

Update a file version if it exists

Updates a file version if a file with such a name already exists.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **SettingsRequestDto** | body | [**SettingsRequestDto**](#model-settingsrequestdto) |  | [optional] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Boolean value: true if the operation is successful | [**BooleanWrapper**](#model-booleanwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**BooleanWrapper**](#model-booleanwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

### uploadDefaultTemplate

> DefaultTemplateSettingsWrapper uploadDefaultTemplate(FileExtension, File)

`POST /api/2.0/files/settings/defaulttemplate`

Upload a file as the default template setting

Uploads a file to use as the default template setting.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **FileExtension** | query | **String** | File extension of a template to replace | [required] [example: .docx] |
| **File** | form | **File** (binary) | File to replace template with | [required] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | New default template settings | [**DefaultTemplateSettingsWrapper**](#model-defaulttemplatesettingswrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **400** | Incorrect or missing file | - | - |
| **403** | You don&#39;t have enough permission to perform the operation | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**DefaultTemplateSettingsWrapper**](#model-defaulttemplatesettingswrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: multipart/form-data
- **Accept**: application/json

## FilesSharingApi

### applyExternalSharePassword

> ExternalShareWrapper applyExternalSharePassword(key, ExternalShareRequestParam)

`POST /api/2.0/files/share/{key}/password`

Apply external data password

Applies a password specified in the request to get the external data.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **key** | path | **String** | The unique document identifier. | [required] [example: doc_key_123] |
| **ExternalShareRequestParam** | body | [**ExternalShareRequestParam**](#model-externalsharerequestparam) | The external data share request parameters. | [required] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | External data | [**ExternalShareWrapper**](#model-externalsharewrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **429** | Too many requests | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**ExternalShareWrapper**](#model-externalsharewrapper)

#### Authorization

No authorization required

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

### changeFileOwner

> FileEntryBaseArrayWrapper changeFileOwner(ChangeOwnerRequestDto)

`POST /api/2.0/files/owner`

Change the file owner

Changes the owner of the file with the ID specified in the request.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **ChangeOwnerRequestDto** | body | [**ChangeOwnerRequestDto**](#model-changeownerrequestdto) |  | [optional] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | File entry information | [**FileEntryBaseArrayWrapper**](#model-fileentrybasearraywrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**FileEntryBaseArrayWrapper**](#model-fileentrybasearraywrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

### getEncryptionAccess

> EncryptionKeyArrayWrapper getEncryptionAccess(fileId)

`GET /api/2.0/files/file/{fileId}/publickeys`

Get file encryption keys

Returns the encryption keys to access a file with the ID specified in the request.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **fileId** | path | **Integer** (int32) | The file unique identifier. | [required] [example: 1] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | List of encryption key pairs: encrypted private key, public key, user ID | [**EncryptionKeyArrayWrapper**](#model-encryptionkeyarraywrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **403** | You do not have enough permissions to edit the file | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**EncryptionKeyArrayWrapper**](#model-encryptionkeyarraywrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### getExternalShareData

> ExternalShareWrapper getExternalShareData(key, fileId, folderId)

`GET /api/2.0/files/share/{key}`

Get the external data

Returns the external data by the key specified in the request.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **key** | path | **String** | The unique key of the external shared data. | [required] [example: doc_key_123] |
| **fileId** | query | **String** | The unique document identifier. | [optional] [example: 1] |
| **folderId** | query | **String** | The unique folder identifier. | [optional] [example: 1] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | External data | [**ExternalShareWrapper**](#model-externalsharewrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**ExternalShareWrapper**](#model-externalsharewrapper)

#### Authorization

No authorization required

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### getFileSecurityInfo

> FileShareArrayWrapper getFileSecurityInfo(id, count, startIndex)

`GET /api/2.0/files/file/{id}/share`

Get the shared file information

Returns the detailed information about the shared file with the ID specified in the request.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **id** | path | **Integer** (int32) | The file unique identifier. | [required] [example: 10] |
| **count** | query | **Integer** (int32) | The number of items to retrieve in the request. | [optional] [example: 25] [min: 1] [max: 100] |
| **startIndex** | query | **Integer** (int32) | The starting index for the query results. | [optional] [example: 0] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | List of shared file information | [**FileShareArrayWrapper**](#model-filesharearraywrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**FileShareArrayWrapper**](#model-filesharearraywrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### getFolderSecurityInfo

> FileShareArrayWrapper getFolderSecurityInfo(id, count, startIndex)

`GET /api/2.0/files/folder/{id}/share`

Get the shared folder information

Returns the detailed information about the shared folder with the ID specified in the request.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **id** | path | **Integer** (int32) | The folder unique identifier. | [required] [example: 10] |
| **count** | query | **Integer** (int32) | The number of items to retrieve in the request. | [optional] [example: 25] [min: 1] [max: 100] |
| **startIndex** | query | **Integer** (int32) | The starting index for the query results. | [optional] [example: 0] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | List of shared file information | [**FileShareArrayWrapper**](#model-filesharearraywrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**FileShareArrayWrapper**](#model-filesharearraywrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### getGroupsMembersWithFileSecurity

> GroupMemberSecurityRequestArrayWrapper getGroupsMembersWithFileSecurity(fileId, groupId, count, startIndex, filterValue)

`GET /api/2.0/files/file/{fileId}/group/{groupId}/share`

Get file group members with security information

Returns the group members with their file security information.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **fileId** | path | **Integer** (int32) | The file ID. | [required] [example: 1] |
| **groupId** | path | **UUID** (uuid) | The group ID. | [required] [example: 00000000-0000-0000-0000-000000000000] |
| **count** | query | **Integer** (int32) | The number of items to be retrieved in the current query. | [optional] [example: 25] [min: 1] [max: 100] |
| **startIndex** | query | **Integer** (int32) | The starting index for the query result set. | [optional] [example: 0] |
| **filterValue** | query | **String** | The filter value used for searching or querying group members based on text input. | [optional] [example: My Document] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Ok | [**GroupMemberSecurityRequestArrayWrapper**](#model-groupmembersecurityrequestarraywrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**GroupMemberSecurityRequestArrayWrapper**](#model-groupmembersecurityrequestarraywrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### getGroupsMembersWithFolderSecurity

> GroupMemberSecurityRequestArrayWrapper getGroupsMembersWithFolderSecurity(folderId, groupId, count, startIndex, filterValue)

`GET /api/2.0/files/folder/{folderId}/group/{groupId}/share`

Get folder group members with security information

Returns the group members with their folder security information.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **folderId** | path | **Integer** (int32) | The folder ID. | [required] [example: 1] |
| **groupId** | path | **UUID** (uuid) | The group ID. | [required] [example: 00000000-0000-0000-0000-000000000000] |
| **count** | query | **Integer** (int32) | The number of items to be retrieved in the current query. | [optional] [example: 25] [min: 1] [max: 100] |
| **startIndex** | query | **Integer** (int32) | The starting index for the query result set. | [optional] [example: 0] |
| **filterValue** | query | **String** | The filter value used for searching or querying group members based on text input. | [optional] [example: My Document] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Ok | [**GroupMemberSecurityRequestArrayWrapper**](#model-groupmembersecurityrequestarraywrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**GroupMemberSecurityRequestArrayWrapper**](#model-groupmembersecurityrequestarraywrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### getSecurityInfo

> FileShareArrayWrapper getSecurityInfo(BaseBatchRequestDto)

`POST /api/2.0/files/share`

Get the sharing rights

Returns the sharing rights for all the files and folders specified in the request.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **BaseBatchRequestDto** | body | [**BaseBatchRequestDto**](#model-basebatchrequestdto) |  | [optional] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | List of shared files and folders information | [**FileShareArrayWrapper**](#model-filesharearraywrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**FileShareArrayWrapper**](#model-filesharearraywrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

### getSharedUsers

> MentionWrapperArrayWrapper getSharedUsers(fileId)

`GET /api/2.0/files/file/{fileId}/sharedusers`

Get user access rights by file ID

Returns a list of users with their access rights to the file with the ID specified in the request.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **fileId** | path | **Integer** (int32) | The file unique identifier. | [required] [example: 1] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | List of users with their access rights to the file | [**MentionWrapperArrayWrapper**](#model-mentionwrapperarraywrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**MentionWrapperArrayWrapper**](#model-mentionwrapperarraywrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### removeSecurityInfo

> BooleanWrapper removeSecurityInfo(BaseBatchRequestDto)

`DELETE /api/2.0/files/share`

Remove the sharing rights

Removes the sharing rights from all the files and folders specified in the request.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **BaseBatchRequestDto** | body | [**BaseBatchRequestDto**](#model-basebatchrequestdto) |  | [optional] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Boolean value: true if the operation is successful | [**BooleanWrapper**](#model-booleanwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**BooleanWrapper**](#model-booleanwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

### sendEditorNotify

> AceShortWrapperArrayWrapper sendEditorNotify(fileId, MentionMessageWrapper)

`POST /api/2.0/files/file/{fileId}/sendeditornotify`

Send the mention message

Sends a message to the users who are mentioned in the file with the ID specified in the request.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **fileId** | path | **Integer** (int32) | The file ID with the mention message. | [required] |
| **MentionMessageWrapper** | body | [**MentionMessageWrapper**](#model-mentionmessagewrapper) | The mention message. | [optional] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | List of access rights information | [**AceShortWrapperArrayWrapper**](#model-aceshortwrapperarraywrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **400** | The list of email addresses is empty | - | - |
| **403** | You don&#39;t have enough permission to perform the operation | - | - |
| **404** | The required file was not found | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**AceShortWrapperArrayWrapper**](#model-aceshortwrapperarraywrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

### setFileSecurityInfo

> FileShareArrayWrapper setFileSecurityInfo(fileId, SecurityInfoSimpleRequestDto)

`PUT /api/2.0/files/file/{fileId}/share`

Share a file

Sets the sharing settings to a file with the ID specified in the request.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **fileId** | path | **Integer** (int32) | The file ID. | [required] [example: 1] |
| **SecurityInfoSimpleRequestDto** | body | [**SecurityInfoSimpleRequestDto**](#model-securityinfosimplerequestdto) | The parameters of the security information simple request. | [required] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | List of shared file information: sharing rights, a user who has the access to the specified file, the file is locked by this user or not, this user is an owner of the specified file or not, this user can edit the access to the specified file or not | [**FileShareArrayWrapper**](#model-filesharearraywrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**FileShareArrayWrapper**](#model-filesharearraywrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

### setFolderSecurityInfo

> FileShareArrayWrapper setFolderSecurityInfo(folderId, SecurityInfoSimpleRequestDto)

`PUT /api/2.0/files/folder/{folderId}/share`

Share a folder

Sets the sharing settings to a folder with the ID specified in the request.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **folderId** | path | **Integer** (int32) | The folder ID. | [required] [example: 1] |
| **SecurityInfoSimpleRequestDto** | body | [**SecurityInfoSimpleRequestDto**](#model-securityinfosimplerequestdto) | The parameters of the security information simple request. | [required] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | List of shared folder information: sharing rights, a user who has the access to the specified folder, the folder is locked by this user or not, this user is an owner of the specified folder or not, this user can edit the access to the specified folder or not | [**FileShareArrayWrapper**](#model-filesharearraywrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**FileShareArrayWrapper**](#model-filesharearraywrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

### setSecurityInfo

> FileShareArrayWrapper setSecurityInfo(SecurityInfoRequestDto)

`PUT /api/2.0/files/share`

Set the sharing rights

Sets the sharing rights to all the files and folders specified in the request.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **SecurityInfoRequestDto** | body | [**SecurityInfoRequestDto**](#model-securityinforequestdto) |  | [optional] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | List of shared files and folders information: sharing rights, a user who has the access to the specified folder, the folder is locked by this user or not, this user is an owner of the specified folder or not, this user can edit the access to the specified folder or not | [**FileShareArrayWrapper**](#model-filesharearraywrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**FileShareArrayWrapper**](#model-filesharearraywrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

## FilesThirdPartyIntegrationApi

### deleteThirdParty

> StringWrapper deleteThirdParty(providerId)

`DELETE /api/2.0/files/thirdparty/{providerId}`

Remove a third-party account

Removes the third-party storage service account with the ID specified in the request.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **providerId** | path | **Integer** (int32) | The provider ID. | [required] [example: 1] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Third-party folder ID | [**StringWrapper**](#model-stringwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**StringWrapper**](#model-stringwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### getAllProviders

> ProviderArrayWrapper getAllProviders(excludewebdav)

`GET /api/2.0/files/thirdparty/providers`

Get all providers

Returns a list of all providers.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **excludewebdav** | query | **Boolean** | Specifies whether WebDAV resources should be excluded from the result.. | [optional] [example: false] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | List of provider | [**ProviderArrayWrapper**](#model-providerarraywrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**ProviderArrayWrapper**](#model-providerarraywrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### getBackupThirdPartyAccount

> FolderStringWrapper getBackupThirdPartyAccount()

`GET /api/2.0/files/thirdparty/backup`

Get a third-party account backup

Returns a backup of the connected third-party account.

#### Parameters
This endpoint does not need any parameter.

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Folder for the third-party account backup | [**FolderStringWrapper**](#model-folderstringwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**FolderStringWrapper**](#model-folderstringwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### getCapabilities

> ArrayArrayWrapper getCapabilities()

`GET /api/2.0/files/thirdparty/capabilities`

Get providers

Returns the list of the available providers.

#### Parameters
This endpoint does not need any parameter.

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | List of provider keys | [**ArrayArrayWrapper**](#model-arrayarraywrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**ArrayArrayWrapper**](#model-arrayarraywrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### getCommonThirdPartyFolders

> FolderStringArrayWrapper getCommonThirdPartyFolders()

`GET /api/2.0/files/thirdparty/common`

Get the common third-party services

Returns a list of the third-party services connected to the Common section.

#### Parameters
This endpoint does not need any parameter.

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | List of common third-party folderst | [**FolderStringArrayWrapper**](#model-folderstringarraywrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**FolderStringArrayWrapper**](#model-folderstringarraywrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### getThirdPartyAccounts

> ThirdPartyParamsArrayWrapper getThirdPartyAccounts()

`GET /api/2.0/files/thirdparty`

Get the third-party accounts

Returns a list of all the connected third-party accounts.

#### Parameters
This endpoint does not need any parameter.

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | List of connected providers information | [**ThirdPartyParamsArrayWrapper**](#model-thirdpartyparamsarraywrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**ThirdPartyParamsArrayWrapper**](#model-thirdpartyparamsarraywrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### saveThirdParty

> FolderStringWrapper saveThirdParty(ThirdPartyRequestDto)

`POST /api/2.0/files/thirdparty`

Save a third-party account

Saves the third-party storage service account. For WebDav, Yandex, kDrive and SharePoint, the login and password are used for authentication. For other providers, the authentication is performed using a token received via OAuth 2.0.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **ThirdPartyRequestDto** | body | [**ThirdPartyRequestDto**](#model-thirdpartyrequestdto) |  | [optional] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Connected provider folder | [**FolderStringWrapper**](#model-folderstringwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**FolderStringWrapper**](#model-folderstringwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

### saveThirdPartyBackup

> FolderStringWrapper saveThirdPartyBackup(ThirdPartyBackupRequestDto)

`POST /api/2.0/files/thirdparty/backup`

Save a third-party account backup

Saves a backup of the connected third-party account.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **ThirdPartyBackupRequestDto** | body | [**ThirdPartyBackupRequestDto**](#model-thirdpartybackuprequestdto) |  | [optional] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Folder for the third-party account backup | [**FolderStringWrapper**](#model-folderstringwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**FolderStringWrapper**](#model-folderstringwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

## PrivacyroomApi

### deleteKeys

> EncryptionKeyArrayWrapper deleteKeys(id)

`DELETE /api/2.0/privacyroom/keys/{id}`

Deletes an encryption key and removes it from the system.

Deletes an encryption key and removes it from the system based on the provided key identifier.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **id** | path | **UUID** (uuid) | The unique identifier of the encryption key to be deleted. | [required] [example: 00000000-0000-0000-0000-000000000000] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | OK | [**EncryptionKeyArrayWrapper**](#model-encryptionkeyarraywrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**EncryptionKeyArrayWrapper**](#model-encryptionkeyarraywrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### getUserKeys

> EncryptionKeyArrayWrapper getUserKeys()

`GET /api/2.0/privacyroom/keys`

Retrieves encryption keys associated with the current user.

Retrieves encryption keys associated with the current user.

#### Parameters
This endpoint does not need any parameter.

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | OK | [**EncryptionKeyArrayWrapper**](#model-encryptionkeyarraywrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**EncryptionKeyArrayWrapper**](#model-encryptionkeyarraywrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### getUserKeysForRoom

> EncryptionKeyArrayWrapper getUserKeysForRoom(roomId)

`GET /api/2.0/privacyroom/{roomId}/access`

Retrieves the encryption keys associated with a specific privacy room.

Retrieves the encryption keys associated with a specific privacy room.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **roomId** | path | **Integer** (int32) | The identifier of the privacy room. | [required] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | OK | [**EncryptionKeyArrayWrapper**](#model-encryptionkeyarraywrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**EncryptionKeyArrayWrapper**](#model-encryptionkeyarraywrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### replaceKey

> EncryptionKeyArrayWrapper replaceKey(EncryptionKeyRequestDto)

`PUT /api/2.0/privacyroom/keys`

Replaces an existing encryption key with a new one for the user.

Replaces an existing encryption key with a new one for the user.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **EncryptionKeyRequestDto** | body | [**EncryptionKeyRequestDto**](#model-encryptionkeyrequestdto) | The request object containing the public and private key information to replace the existing key. | [optional] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | OK | [**EncryptionKeyArrayWrapper**](#model-encryptionkeyarraywrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**EncryptionKeyArrayWrapper**](#model-encryptionkeyarraywrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

### setKeys

> EncryptionKeyArrayWrapper setKeys(EncryptionKeyRequestDto)

`POST /api/2.0/privacyroom/keys`

Creates and sets encryption keys for the user.

Creates and sets encryption keys for the user.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **EncryptionKeyRequestDto** | body | [**EncryptionKeyRequestDto**](#model-encryptionkeyrequestdto) | The request object containing public and private key information. | [optional] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | OK | [**EncryptionKeyArrayWrapper**](#model-encryptionkeyarraywrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**EncryptionKeyArrayWrapper**](#model-encryptionkeyarraywrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

## RoomsApi

### addRoomTags

> FolderIntegerWrapper addRoomTags(id, BatchTagsRequestDto)

`PUT /api/2.0/files/rooms/{id}/tags`

Add the room tags

Adds the tags to a room with the ID specified in the request.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **id** | path | **Integer** (int32) | The room Id. | [required] [example: 1] |
| **BatchTagsRequestDto** | body | [**BatchTagsRequestDto**](#model-batchtagsrequestdto) | The parameters for managing tags. | [optional] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Room information | [**FolderIntegerWrapper**](#model-folderintegerwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **403** | You don&#39;t have permission to edit the room | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**FolderIntegerWrapper**](#model-folderintegerwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

### archiveRoom

> FileOperationWrapper archiveRoom(id, ArchiveRoomRequest)

`PUT /api/2.0/files/rooms/{id}/archive`

Archive a room

Moves a room with the ID specified in the request to the Archive section.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **id** | path | **Integer** (int32) | The room ID. | [required] [example: 1] |
| **ArchiveRoomRequest** | body | [**ArchiveRoomRequest**](#model-archiveroomrequest) | The parameters for archiving a room. | [optional] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | File operation | [**FileOperationWrapper**](#model-fileoperationwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**FileOperationWrapper**](#model-fileoperationwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

### changeRoomCover

> FolderIntegerWrapper changeRoomCover(id, CoverRequestDto)

`POST /api/2.0/files/rooms/{id}/cover`

Change the room cover

Changes a cover of a room with the ID specified in the request.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **id** | path | **Integer** (int32) | The room ID. | [required] [example: 1] |
| **CoverRequestDto** | body | [**CoverRequestDto**](#model-coverrequestdto) | The request parameters to change the room cover. | [required] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Room cover | [**FolderIntegerWrapper**](#model-folderintegerwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **403** | You don&#39;t have permission to change cover | - | - |
| **404** | The required room was not found | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**FolderIntegerWrapper**](#model-folderintegerwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

### createRoom

> FolderIntegerWrapper createRoom(CreateRoomRequestDto)

`POST /api/2.0/files/rooms`

Create a room

Creates a room in the Rooms section.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **CreateRoomRequestDto** | body | [**CreateRoomRequestDto**](#model-createroomrequestdto) |  | [optional] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Room information | [**FolderIntegerWrapper**](#model-folderintegerwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**FolderIntegerWrapper**](#model-folderintegerwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

### createRoomFromTemplate

> RoomFromTemplateStatusWrapper createRoomFromTemplate(CreateRoomFromTemplateDto)

`POST /api/2.0/files/rooms/fromtemplate`

Create a room from the template

Creates a room in the Rooms section based on the template.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **CreateRoomFromTemplateDto** | body | [**CreateRoomFromTemplateDto**](#model-createroomfromtemplatedto) |  | [optional] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Status | [**RoomFromTemplateStatusWrapper**](#model-roomfromtemplatestatuswrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**RoomFromTemplateStatusWrapper**](#model-roomfromtemplatestatuswrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

### createRoomLogo

> FolderIntegerWrapper createRoomLogo(id, LogoRequest)

`POST /api/2.0/files/rooms/{id}/logo`

Create a room logo

Creates a logo for a room with the ID specified in the request.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **id** | path | **Integer** (int32) | The room ID. | [required] [example: 1] |
| **LogoRequest** | body | [**LogoRequest**](#model-logorequest) | The logo request parameters. | [required] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Room information | [**FolderIntegerWrapper**](#model-folderintegerwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **404** | The required room was not found | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**FolderIntegerWrapper**](#model-folderintegerwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

### createRoomTag

> StringWrapper createRoomTag(CreateTagRequestDto)

`POST /api/2.0/files/tags`

Create a room tag

Creates a custom room tag with the parameters specified in the request.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **CreateTagRequestDto** | body | [**CreateTagRequestDto**](#model-createtagrequestdto) |  | [optional] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | New tag name | [**StringWrapper**](#model-stringwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **403** | You don&#39;t have enough permission to perform the operation | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**StringWrapper**](#model-stringwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

### createRoomTemplate

> RoomTemplateStatusWrapper createRoomTemplate(RoomTemplateDto)

`POST /api/2.0/files/roomtemplate`

Start creating room template

Starts creating the room template.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **RoomTemplateDto** | body | [**RoomTemplateDto**](#model-roomtemplatedto) |  | [optional] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Status | [**RoomTemplateStatusWrapper**](#model-roomtemplatestatuswrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**RoomTemplateStatusWrapper**](#model-roomtemplatestatuswrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

### createRoomThirdParty

> FolderStringWrapper createRoomThirdParty(id, CreateThirdPartyRoom)

`POST /api/2.0/files/rooms/thirdparty/{id}`

Create a third-party room

Creates a room in the Rooms section stored in a third-party storage.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **id** | path | **String** | The ID of the folder in the third-party storage in which the contents of the room will be stored. | [required] [example: folder-123-abc] |
| **CreateThirdPartyRoom** | body | [**CreateThirdPartyRoom**](#model-createthirdpartyroom) | The third-party room information. | [required] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Room information | [**FolderStringWrapper**](#model-folderstringwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**FolderStringWrapper**](#model-folderstringwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

### deleteCustomTags

> deleteCustomTags(BatchTagsRequestDto)

`DELETE /api/2.0/files/tags`

Delete the custom room tags

Deletes a bunch of custom tags specified in the request.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **BatchTagsRequestDto** | body | [**BatchTagsRequestDto**](#model-batchtagsrequestdto) |  | [optional] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Ok | - | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **403** | You don&#39;t have enough permission to perform the operation | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

null (empty response body)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: Not defined

### deleteRoom

> FileOperationWrapper deleteRoom(id, DeleteRoomRequest)

`DELETE /api/2.0/files/rooms/{id}`

Remove a room

Removes a room with the ID specified in the request.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **id** | path | **Integer** (int32) | The room ID. | [required] [example: 10] |
| **DeleteRoomRequest** | body | [**DeleteRoomRequest**](#model-deleteroomrequest) | The parameters for deleting a room. | [required] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | File operation | [**FileOperationWrapper**](#model-fileoperationwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**FileOperationWrapper**](#model-fileoperationwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

### deleteRoomLogo

> FolderIntegerWrapper deleteRoomLogo(id)

`DELETE /api/2.0/files/rooms/{id}/logo`

Remove a room logo

Removes a logo from a room with the ID specified in the request.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **id** | path | **Integer** (int32) | The room ID. | [required] [example: 1] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Room information | [**FolderIntegerWrapper**](#model-folderintegerwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**FolderIntegerWrapper**](#model-folderintegerwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### deleteRoomTags

> FolderIntegerWrapper deleteRoomTags(id, BatchTagsRequestDto)

`DELETE /api/2.0/files/rooms/{id}/tags`

Remove the room tags

Removes the tags from a room with the ID specified in the request.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **id** | path | **Integer** (int32) | The room Id. | [required] [example: 1] |
| **BatchTagsRequestDto** | body | [**BatchTagsRequestDto**](#model-batchtagsrequestdto) | The parameters for managing tags. | [optional] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Room information | [**FolderIntegerWrapper**](#model-folderintegerwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **403** | You don&#39;t have permission to edit the room | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**FolderIntegerWrapper**](#model-folderintegerwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

### getExternalDbSyncStatus

> ExternalDbSyncTaskWrapper getExternalDbSyncStatus(id)

`GET /api/2.0/files/rooms/{id}/externaldbsync`

Get external DB sync status

Returns the status of the external DB synchronization task for the specified filling forms room.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **id** | path | **Integer** (int32) | The room ID. | [required] [example: 1] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Synchronization task information | [**ExternalDbSyncTaskWrapper**](#model-externaldbsynctaskwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **404** | Room not found | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**ExternalDbSyncTaskWrapper**](#model-externaldbsynctaskwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### getNewRoomItems

> NewItemsFileEntryBaseArrayWrapper getNewRoomItems(id)

`GET /api/2.0/files/rooms/{id}/news`

Get the new room items

Returns a list of all the new items from a room with the ID specified in the request.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **id** | path | **Integer** (int32) | The room ID. | [required] [example: 1] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | List of file entry information | [**NewItemsFileEntryBaseArrayWrapper**](#model-newitemsfileentrybasearraywrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**NewItemsFileEntryBaseArrayWrapper**](#model-newitemsfileentrybasearraywrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### getPublicSettings

> BooleanWrapper getPublicSettings(id)

`GET /api/2.0/files/roomtemplate/{id}/public`

Get public settings

Returns the public settings of the room template with the ID specified in the request.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **id** | path | **Integer** (int32) | The room template ID. | [required] [example: 1] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Ok | [**BooleanWrapper**](#model-booleanwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**BooleanWrapper**](#model-booleanwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### getRoomCovers

> CoversResultArrayWrapper getRoomCovers()

`GET /api/2.0/files/rooms/covers`

Get covers

Returns a list of all covers.

#### Parameters
This endpoint does not need any parameter.

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Gets room cover | [**CoversResultArrayWrapper**](#model-coversresultarraywrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**CoversResultArrayWrapper**](#model-coversresultarraywrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### getRoomCreatingStatus

> RoomFromTemplateStatusWrapper getRoomCreatingStatus()

`GET /api/2.0/files/rooms/fromtemplate/status`

Get the room creation progress

Returns the progress of creating a room from the template.

#### Parameters
This endpoint does not need any parameter.

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Status | [**RoomFromTemplateStatusWrapper**](#model-roomfromtemplatestatuswrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**RoomFromTemplateStatusWrapper**](#model-roomfromtemplatestatuswrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### getRoomIndexExport

> DocumentBuilderTaskWrapper getRoomIndexExport()

`GET /api/2.0/files/rooms/indexexport`

Get the room index export

Returns the room index export.

#### Parameters
This endpoint does not need any parameter.

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Ok | [**DocumentBuilderTaskWrapper**](#model-documentbuildertaskwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**DocumentBuilderTaskWrapper**](#model-documentbuildertaskwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### getRoomInfo

> FolderIntegerWrapper getRoomInfo(id)

`GET /api/2.0/files/rooms/{id}`

Get room information

Returns the room information.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **id** | path | **Integer** (int32) | The room ID. | [required] [example: 1] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Room information | [**FolderIntegerWrapper**](#model-folderintegerwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**FolderIntegerWrapper**](#model-folderintegerwrapper)

#### Authorization

No authorization required

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### getRoomLinks

> FileShareArrayWrapper getRoomLinks(id, type)

`GET /api/2.0/files/rooms/{id}/links`

Get the room links

Returns the links of the room with the ID specified in the request.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **id** | path | **Integer** (int32) | The room ID. | [required] [example: 1] |
| **type** | query | **LinkType** | The link type. | [optional] [example: 1] [enum: 0, 1] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Room security information | [**FileShareArrayWrapper**](#model-filesharearraywrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**FileShareArrayWrapper**](#model-filesharearraywrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### getRoomSecurityInfo

> FileShareArrayWrapper getRoomSecurityInfo(id, filterType, count, startIndex, filterValue)

`GET /api/2.0/files/rooms/{id}/share`

Get the room access rights

Returns the access rights of a room with the ID specified in the request.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **id** | path | **Integer** (int32) | The room ID. | [required] [example: 1] |
| **filterType** | query | **ShareFilterType** | The filter type of the access rights. | [optional] [example: 1] [enum: 0, 1, 2, 4, 8, 15, 16, 32] |
| **count** | query | **Integer** (int32) | The number of items to be retrieved or processed. | [optional] [example: 25] [min: 1] [max: 100] |
| **startIndex** | query | **Integer** (int32) | The starting index of the items to retrieve in a paginated request. | [optional] [example: 0] |
| **filterValue** | query | **String** | The text filter value used for filtering room security information. | [optional] [example: Sample filter] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Security information of room files | [**FileShareArrayWrapper**](#model-filesharearraywrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**FileShareArrayWrapper**](#model-filesharearraywrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### getRoomTagsInfo

> ObjectArrayWrapper getRoomTagsInfo(count, startIndex, filterValue)

`GET /api/2.0/files/tags`

Get the room tags

Returns a list of custom tags.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **count** | query | **Integer** (int32) | Gets or sets the number of tag results to retrieve.  This property specifies the maximum amount of tag data to be included in the result set. | [optional] [example: 25] [min: 1] [max: 100] |
| **startIndex** | query | **Integer** (int32) | Represents the starting index from which the tags&#39; information will be retrieved.  This property is used to define the offset for pagination when retrieving a list of tags. It determines  the point in the data set from which the retrieval begins. | [optional] [example: 0] |
| **filterValue** | query | **String** | Gets or sets the text value used for searching tags.  This property is typically used as a filter value when retrieving tag information. | [optional] [example: My Document] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | List of tag names | [**ObjectArrayWrapper**](#model-objectarraywrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**ObjectArrayWrapper**](#model-objectarraywrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### getRoomTemplateCreatingStatus

> RoomTemplateStatusWrapper getRoomTemplateCreatingStatus()

`GET /api/2.0/files/roomtemplate/status`

Get status of room template creation

Returns the progress status of the room template creation process.

#### Parameters
This endpoint does not need any parameter.

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Status | [**RoomTemplateStatusWrapper**](#model-roomtemplatestatuswrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**RoomTemplateStatusWrapper**](#model-roomtemplatestatuswrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### getRoomsFolder

> FolderContentIntegerWrapper getRoomsFolder(type, subjectId, subjectOwnerId, searchArea, withoutTags, tags, excludeSubject, provider, quotaFilter, storageFilter, privacyFilter, count, startIndex, sortBy, sortOrder, filterValue, groupId)

`GET /api/2.0/files/rooms`

Get rooms

Returns the contents of the Rooms section by the parameters specified in the request.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **type** | query | [**List**](#model-roomtype) | The filter by room type. | [optional] [example: 1] |
| **subjectId** | query | **UUID** (uuid) | The filter by user ID. | [optional] [example: 00000000-0000-0000-0000-000000000000] |
| **subjectOwnerId** | query | **UUID** (uuid) | The filter by room owner ID. | [optional] [example: 00000000-0000-0000-0000-000000000000] |
| **searchArea** | query | **SearchArea** | The room search area (Active, Archive, Any, Recent by links). | [optional] [example: 1] [enum: 0, 1, 2, 3, 4, 5, 6, 7, 8, 9] |
| **withoutTags** | query | **Boolean** | Specifies whether to search by tags or not. | [optional] [example: false] |
| **tags** | query | **String** | The tags in the serialized format. | [optional] [example: tag1] |
| **excludeSubject** | query | **Boolean** | Specifies whether to exclude search by user or group ID. | [optional] [example: false] |
| **provider** | query | **ProviderFilter** | The filter by provider name (None, Box, DropBox, GoogleDrive, kDrive, OneDrive, SharePoint, WebDav, Yandex, Storage). | [optional] [example: 1] [enum: 0, 1, 2, 3, 4, 5, 6, 7, 8, 9] |
| **quotaFilter** | query | **QuotaFilter** | The filter by quota (All - 0, Default - 1, Custom - 2). | [optional] [example: 1] [enum: 0, 1, 2] |
| **storageFilter** | query | **StorageFilter** | The filter by storage (None - 0, Internal - 1, ThirdParty - 2). | [optional] [example: 1] [enum: 0, 1, 2] |
| **privacyFilter** | query | **RoomPrivacyFilter** | The filter by room privacy (None - 0, Private - 1, NotPrivate - 2). When omitted, all rooms are returned. | [optional] [example: 1] [enum: 0, 1, 2] |
| **count** | query | **Integer** (int32) | Specifies the maximum number of items to retrieve. | [optional] [example: 25] [min: 1] [max: 100] |
| **startIndex** | query | **Integer** (int32) | The index from which to start retrieving the room content. | [optional] [example: 0] |
| **sortBy** | query | **String** | Specifies the field by which the room content should be sorted. | [optional] [example: DateAndTime] |
| **sortOrder** | query | **SortOrder** | The order in which the results are sorted. | [optional] [example: 1] [enum: 0, 1] |
| **filterValue** | query | **String** | The text filter value used to refine search or query operations. | [optional] [example: My Document] |
| **groupId** | query | **Integer** (int32) | The group ID | [optional] [example: 1] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Returns the contents of the Rooms section | [**FolderContentIntegerWrapper**](#model-foldercontentintegerwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **403** | You don&#39;t have enough permission to view the room content | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**FolderContentIntegerWrapper**](#model-foldercontentintegerwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### getRoomsNewItems

> NewItemsRoomNewItemsArrayWrapper getRoomsNewItems()

`GET /api/2.0/files/rooms/news`

Get the room new items

Returns the room new items.

#### Parameters
This endpoint does not need any parameter.

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | List of new items | [**NewItemsRoomNewItemsArrayWrapper**](#model-newitemsroomnewitemsarraywrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**NewItemsRoomNewItemsArrayWrapper**](#model-newitemsroomnewitemsarraywrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### getRoomsPrimaryExternalLink

> FileShareWrapper getRoomsPrimaryExternalLink(id)

`GET /api/2.0/files/rooms/{id}/link`

Get the room primary external link

Returns the primary external link of the room with the ID specified in the request.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **id** | path | **Integer** (int32) | The room ID. | [required] [example: 1] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Room security information | [**FileShareWrapper**](#model-filesharewrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **403** | You don&#39;t have enough permission to perform the operation | - | - |
| **404** | Not Found | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**FileShareWrapper**](#model-filesharewrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### hasTagLinks

> BooleanWrapper hasTagLinks(tagName2, tagName)

`GET /api/2.0/files/tags/{tagName}/haslinks`

Has tag links

Checks if a specific custom tag has linked items.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **tagName2** | path | **String** |  | [required] |
| **tagName** | query | **String** | Represents the name of a tag | [optional] [example: tag1] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | True if tag has links, false otherwise | [**BooleanWrapper**](#model-booleanwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **404** | Tag not found | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**BooleanWrapper**](#model-booleanwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### pinRoom

> FolderIntegerWrapper pinRoom(id)

`PUT /api/2.0/files/rooms/{id}/pin`

Pin a room

Pins a room with the ID specified in the request to the top of the list.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **id** | path | **Integer** (int32) | The room ID. | [required] [example: 1] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Room information | [**FolderIntegerWrapper**](#model-folderintegerwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**FolderIntegerWrapper**](#model-folderintegerwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### reorderRoom

> FolderIntegerWrapper reorderRoom(id)

`PUT /api/2.0/files/rooms/{id}/reorder`

Reorder the room

Reorders the room with ID specified in the request.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **id** | path | **Integer** (int32) | The room ID. | [required] [example: 1] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Room information | [**FolderIntegerWrapper**](#model-folderintegerwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**FolderIntegerWrapper**](#model-folderintegerwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### resendEmailInvitations

> resendEmailInvitations(id, UserInvitation)

`POST /api/2.0/files/rooms/{id}/resend`

Resend the room invitations

Resends the email invitations to a room with the ID specified in the request to the selected users.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **id** | path | **Integer** (int32) | The room ID. | [required] [example: 1] |
| **UserInvitation** | body | [**UserInvitation**](#model-userinvitation) | The user invitation parameters. | [required] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Ok | - | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

null (empty response body)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: Not defined

### setPublicSettings

> setPublicSettings(SetPublicDto)

`PUT /api/2.0/files/roomtemplate/public`

Set public settings

Sets the public settings for the room template with the ID specified in the request.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **SetPublicDto** | body | [**SetPublicDto**](#model-setpublicdto) |  | [optional] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Ok | - | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

null (empty response body)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: Not defined

### setRoomLink

> FileShareWrapper setRoomLink(id, RoomLinkRequest)

`PUT /api/2.0/files/rooms/{id}/links`

Set the room external or invitation link

Sets the room external or invitation link with the ID specified in the request.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **id** | path | **Integer** (int32) | The room ID. | [required] [example: 1] |
| **RoomLinkRequest** | body | [**RoomLinkRequest**](#model-roomlinkrequest) | The room link parameters. | [required] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Room security information | [**FileShareWrapper**](#model-filesharewrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**FileShareWrapper**](#model-filesharewrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

### setRoomSecurity

> RoomSecurityWrapper setRoomSecurity(id, RoomInvitationRequest)

`PUT /api/2.0/files/rooms/{id}/share`

Set the room access rights

Sets the access rights to the room with the ID specified in the request.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **id** | path | **Integer** (int32) | The room ID. | [required] [example: 1] |
| **RoomInvitationRequest** | body | [**RoomInvitationRequest**](#model-roominvitationrequest) | The room invitation request. | [required] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Room security information | [**RoomSecurityWrapper**](#model-roomsecuritywrapper) | - |
| **401** | Unauthorized | - | - |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**RoomSecurityWrapper**](#model-roomsecuritywrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

### startExternalDbSync

> ExternalDbSyncTaskWrapper startExternalDbSync(id)

`POST /api/2.0/files/rooms/{id}/externaldbsync`

Start external DB sync

Triggers external DB synchronization for all form templates in the specified filling forms room.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **id** | path | **Integer** (int32) | The room ID. | [required] [example: 1] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Synchronization task information | [**ExternalDbSyncTaskWrapper**](#model-externaldbsynctaskwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **400** | External DB is not configured | - | - |
| **403** | You do not have enough permissions to perform this action | - | - |
| **404** | Room not found | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**ExternalDbSyncTaskWrapper**](#model-externaldbsynctaskwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### startRoomIndexExport

> DocumentBuilderTaskWrapper startRoomIndexExport(id)

`POST /api/2.0/files/rooms/{id}/indexexport`

Start the room index export

Starts the index export of a room with the ID specified in the request.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **id** | path | **Integer** (int32) | The room ID. | [required] [example: 1] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Ok | [**DocumentBuilderTaskWrapper**](#model-documentbuildertaskwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **501** | Folder indexing is turned off | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**DocumentBuilderTaskWrapper**](#model-documentbuildertaskwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### terminateRoomIndexExport

> terminateRoomIndexExport()

`DELETE /api/2.0/files/rooms/indexexport`

Terminate the room index export

Terminates the room index export.

#### Parameters
This endpoint does not need any parameter.

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Ok | - | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

null (empty response body)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: Not defined

### unarchiveRoom

> FileOperationWrapper unarchiveRoom(id, ArchiveRoomRequest)

`PUT /api/2.0/files/rooms/{id}/unarchive`

Unarchive a room

Moves a room with the ID specified in the request from the Archive section to the Rooms section.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **id** | path | **Integer** (int32) | The room ID. | [required] [example: 1] |
| **ArchiveRoomRequest** | body | [**ArchiveRoomRequest**](#model-archiveroomrequest) | The parameters for archiving a room. | [optional] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | File operation | [**FileOperationWrapper**](#model-fileoperationwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**FileOperationWrapper**](#model-fileoperationwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

### unpinRoom

> FolderIntegerWrapper unpinRoom(id)

`PUT /api/2.0/files/rooms/{id}/unpin`

Unpin a room

Unpins a room with the ID specified in the request from the top of the list.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **id** | path | **Integer** (int32) | The room ID. | [required] [example: 1] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Room information | [**FolderIntegerWrapper**](#model-folderintegerwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**FolderIntegerWrapper**](#model-folderintegerwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### updateRoom

> FolderIntegerWrapper updateRoom(id, UpdateRoomRequest)

`PUT /api/2.0/files/rooms/{id}`

Update a room

Updates a room with the ID specified in the request.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **id** | path | **Integer** (int32) | The room ID. | [required] |
| **UpdateRoomRequest** | body | [**UpdateRoomRequest**](#model-updateroomrequest) | The request parameters for updating a room. | [required] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Updated room information | [**FolderIntegerWrapper**](#model-folderintegerwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**FolderIntegerWrapper**](#model-folderintegerwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

### updateRoomTag

> StringWrapper updateRoomTag(UpdateTagRequestDto)

`PUT /api/2.0/files/tags`

Update tag

Updates the name of a custom tag.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **UpdateTagRequestDto** | body | [**UpdateTagRequestDto**](#model-updatetagrequestdto) |  | [optional] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Updated tag name | [**StringWrapper**](#model-stringwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **403** | You don&#39;t have enough permission to perform the operation | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**StringWrapper**](#model-stringwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

### uploadRoomLogo

> UploadResultWrapper uploadRoomLogo(File)

`POST /api/2.0/files/logos`

Upload a room logo image

Uploads a temporary image to create a room logo.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **File** | form | **File** (binary) | The image data. | [optional] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | Upload result | [**UploadResultWrapper**](#model-uploadresultwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **403** | No permissions to perform this action | - | - |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**UploadResultWrapper**](#model-uploadresultwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: multipart/form-data
- **Accept**: application/json

## RoomsGroupsApi

### addRoomGroup

> RoomGroupWrapper addRoomGroup(RoomGroupRequestDto)

`POST /api/2.0/files/group`

Add a new room group

Creates a new room group with the specified name, icon, and list of rooms.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **RoomGroupRequestDto** | body | [**RoomGroupRequestDto**](#model-roomgrouprequestdto) |  | [optional] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | OK | [**RoomGroupWrapper**](#model-roomgroupwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**RoomGroupWrapper**](#model-roomgroupwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

### changeRoomGroupIcon

> RoomGroupWrapper changeRoomGroupIcon(id, IconRequest)

`POST /api/2.0/files/group/{id}/icon`

Change group icon

Changes the icon of an existing room group.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **id** | path | **Integer** (int32) | Group id | [required] [example: 1] |
| **IconRequest** | body | [**IconRequest**](#model-iconrequest) | Icon update data. | [optional] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | OK | [**RoomGroupWrapper**](#model-roomgroupwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**RoomGroupWrapper**](#model-roomgroupwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json

### deleteRoomGroup

> deleteRoomGroup(id, includeMembers)

`DELETE /api/2.0/files/group/{id}`

Delete group

Deletes the specified room group.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **id** | path | **Integer** (int32) | The group unique identifier. | [required] [example: 10] |
| **includeMembers** | query | **Boolean** | Whether to include group members. | [optional] [example: true] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | OK | - | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

null (empty response body)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: Not defined

### getRoomGroupInfo

> RoomGroupWrapper getRoomGroupInfo(id, includeMembers)

`GET /api/2.0/files/group/{id}`

Get room group info

Returns detailed information about a room group.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **id** | path | **Integer** (int32) | The group unique identifier. | [required] [example: 10] |
| **includeMembers** | query | **Boolean** | Whether to include group members. | [optional] [example: true] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | OK | [**RoomGroupWrapper**](#model-roomgroupwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**RoomGroupWrapper**](#model-roomgroupwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### getRoomGroups

> RoomGroupArrayWrapper getRoomGroups(id, includeMembers)

`GET /api/2.0/files/group`

List room groups

Returns a list of all room groups for the current user.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **id** | path | **Integer** (int32) | The group unique identifier. | [required] [example: 10] |
| **includeMembers** | query | **Boolean** | Whether to include group members. | [optional] [example: true] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | OK | [**RoomGroupArrayWrapper**](#model-roomgrouparraywrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**RoomGroupArrayWrapper**](#model-roomgrouparraywrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: Not defined
- **Accept**: application/json

### updateRoomGroup

> RoomGroupWrapper updateRoomGroup(id, UpdateRoomGroupRequest)

`PUT /api/2.0/files/group/{id}`

Update room group

Updates room group properties and adds or removes rooms.

#### Parameters

|Name | In | Type | Description | Notes |
|------------- | ------------- | ------------- | ------------- | -------------|
| **id** | path | **Integer** (int32) | The group ID. | [required] [example: 1] |
| **UpdateRoomGroupRequest** | body | [**UpdateRoomGroupRequest**](#model-updateroomgrouprequest) | The request for updating a group. | [required] |

#### Responses

| Status code | Description | Type | Response headers |
|------------- | ------------- | ------------- | -------------|
| **200** | OK | [**RoomGroupWrapper**](#model-roomgroupwrapper) | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` |
| **401** | Unauthorized | - | - |
| **429** | Too Many Requests. | - | `Retry-After` |
| **502** | Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |
| **503** | Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON. | - | - |

#### Return type

[**RoomGroupWrapper**](#model-roomgroupwrapper)

#### Authorization

[Basic](#basic), [OAuth2](#oauth2) (scopes: read, write), [ApiKeyBearer](#apikeybearer) (scopes: read, write), [asc_auth_key](#asc_auth_key) (scopes: read, write), [Bearer](#bearer), [OpenId](#openid)

#### HTTP request headers

- **Content-Type**: application/json
- **Accept**: application/json


## Models


### Model AccessRequestKeyDto

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **userId** | **UUID** (uuid) | User ID | [optional] [example: 00000000-0000-0000-0000-000000000000] |
| **publicKeyId** | **UUID** (uuid) | Public key ID | [optional] [example: 00000000-0000-0000-0000-000000000000] |
| **privateKeyEnc** | **String** | Encrypted private key | [optional] [example: encrypted_key_string] [nullable] |


### Model AceShortWrapper
The information about the settings which allow to share the document with other users.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **user** | **String** | The name of the user the document will be shared with. | [optional] [example: John Doe] [nullable] |
| **permissions** | **String** | The access rights for the user with the name above.  Can be Full Access, Read Only, or Deny Access. | [optional] [example: Full Access] [nullable] |
| **isLink** | **Boolean** | Specifies whether to change the user icon to the link icon. | [optional] [example: false] |


### Model AceShortWrapperArrayWrapper

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **response** | [**List**](#model-aceshortwrapper) |  | [optional] |
| **count** | **Integer** (int32) | The total number of items in the response | [optional] |
| **links** | [**List**](#model-booleanwrapperlinks-item) | List of links related to the response | [optional] |
| **status** | **Integer** (int32) | HTTP status code of the response | [optional] |
| **statusCode** | **Integer** (int32) | HTTP status code of the response (duplicate of status) | [optional] |


### Model ActionConfig
The information about the action in the document that will be scrolled to.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **data** | **String** | The action data that will be scrolled to. | [optional] [example: section] [minLength: 0] [maxLength: 256] [nullable] |
| **type** | **String** | The action type. | [optional] [example: scroll] [minLength: 0] [maxLength: 128] [nullable] |


### Model ActionLinkConfig
The config parameter which contains the information about the action in the document that will be scrolled to.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **action** | [**ActionConfig**](#model-actionconfig) |  | [optional] |


### Model AnonymousConfigDto
The anonymous config parameters.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **request** | **Boolean** | Specifies if the anonymous is a request. | [required] [example: false] |


### Model ApiDateTime
The API date and time parameters.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **utcTime** | **Date** (date-time) | The time in UTC format. | [optional] [example: 2018-01-01T00:00:00Z] |
| **timeZoneOffset** | **String** (date-span) | The time zone offset. | [optional] [example: 00:00:00] |


### Model ApplyFilterOption
[0 - All, 1 - Files, 2 - Folders]

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|


### Model ArchiveRoomRequest
The parameters for archiving a room.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **deleteAfter** | **Boolean** | Specifies whether to archive a room after the editing session is finished or not. | [optional] [example: false] |


### Model ArrayArrayWrapper

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **response** | **List** |  | [optional] |
| **count** | **Integer** (int32) | The total number of items in the response | [optional] |
| **links** | [**List**](#model-booleanwrapperlinks-item) | List of links related to the response | [optional] |
| **status** | **Integer** (int32) | HTTP status code of the response | [optional] |
| **statusCode** | **Integer** (int32) | HTTP status code of the response (duplicate of status) | [optional] |


### Model AuditReportFormat
[]

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|


### Model AuthData
The authentication data.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **login** | **String** | The authentication login. | [optional] [example: user@example.com] [nullable] |
| **password** | **String** | The authentication password. | [optional] [example: p@ssw0rd!] [nullable] |
| **rawToken** | **String** | The authentication raw token. | [optional] [example: {"access_token":"eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...","expires_in":3600}] [nullable] |
| **url** | **URI** (uri) | The authentication URL. | [optional] [example: https://auth.example.com] [nullable] |
| **provider** | **String** | The authentication provider. | [optional] [example: OAuth2] [nullable] |
| **token** | [**OAuth20Token**](#model-oauth20token) |  | [optional] |


### Model AutoCleanUpData
The auto-clearing setting parameters.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **isAutoCleanUp** | **Boolean** | Specifies whether to permanently delete files in the Trash folder. | [optional] [example: false] |
| **gap** | [**DateToAutoCleanUp**](#model-datetoautocleanup) |  | [optional] [enum: 1, 2, 3, 4, 5, 6] |


### Model AutoCleanUpDataWrapper

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **response** | [**AutoCleanUpData**](#model-autocleanupdata) |  | [optional] |
| **count** | **Integer** (int32) | The total number of items in the response | [optional] |
| **links** | [**List**](#model-booleanwrapperlinks-item) | List of links related to the response | [optional] |
| **status** | **Integer** (int32) | HTTP status code of the response | [optional] |
| **statusCode** | **Integer** (int32) | HTTP status code of the response (duplicate of status) | [optional] |


### Model AutoCleanupRequestDto
The request parameters for updating the trash bin auto-clearing setting.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **set** | **Boolean** | Specifies whether to enable the auto-clearing or not. | [optional] [example: true] |
| **gap** | [**DateToAutoCleanUp**](#model-datetoautocleanup) |  | [optional] [enum: 1, 2, 3, 4, 5, 6] |


### Model BaseBatchRequestDto
The base batch request parameters.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **returnSingleOperation** | **Boolean** | Specifies whether to return only the current operation | [optional] |
| **folderIds** | [**List**](#model-basebatchrequestdtofolderids) | The list of folder IDs of the base batch request. | [optional] [nullable] |
| **fileIds** | [**List**](#model-basebatchrequestdtofileids) | The list of file IDs of the base batch request. | [optional] [nullable] |


### Model BaseBatchRequestDto.fileIds

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|


### Model BaseBatchRequestDto.folderIds

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|


### Model BatchRequestDto
The request parameters for copying/moving files.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **returnSingleOperation** | **Boolean** | Specifies whether to return only the current operation | [optional] |
| **folderIds** | [**List**](#model-batchrequestdtofolderids) | The list of folder IDs to be copied/moved. | [optional] [nullable] |
| **fileIds** | [**List**](#model-batchrequestdtofileids) | The list of file IDs to be copied/moved. | [optional] [nullable] |
| **destFolderId** | [**BatchRequestDto_allOf_destFolderId**](#model-batchrequestdtodestfolderid) |  | [optional] |
| **conflictResolveType** | [**FileConflictResolveType**](#model-fileconflictresolvetype) |  | [optional] [enum: Skip, Overwrite, Duplicate] |
| **deleteAfter** | **Boolean** | Specifies whether to delete the source files/folders after they are moved or copied to the destination folder. | [optional] |
| **content** | **Boolean** | Specifies whether to copy or move the folder content or not. | [optional] |
| **toFillOut** | **Boolean** | Specifies whether the file is copied for filling out | [optional] |


### Model BatchRequestDto.destFolderId
The destination folder ID.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|


### Model BatchRequestDto.fileIds

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|


### Model BatchRequestDto.folderIds

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|


### Model BatchTagsRequestDto
The parameters for managing room tags.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **names** | **List** | The list of tag names. | [required] [example: ["tag1","tag2","tag3"]] |


### Model BooleanWrapper

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **response** | **Boolean** |  | [optional] |
| **count** | **Integer** (int32) | The total number of items in the response | [optional] |
| **links** | [**List**](#model-booleanwrapperlinks-item) | List of links related to the response | [optional] |
| **status** | **Integer** (int32) | HTTP status code of the response | [optional] |
| **statusCode** | **Integer** (int32) | HTTP status code of the response (duplicate of status) | [optional] |


### Model BooleanWrapper.links item

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **href** | **String** | URL of the link | [optional] |
| **action** | **String** | Action associated with the link | [optional] |


### Model ChangeHistory
The parameters for changing version history.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **version** | **Integer** (int32) | The file version of the change history. | [required] [example: 1] |
| **continueVersion** | **Boolean** | Specifies whether to start a new version or continue revision of the change history. | [optional] [example: false] |


### Model ChangeOwnerRequestDto
The request parameters for changing the file owner.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **folderIds** | [**List**](#model-batchrequestdtofileids) | The list of folder IDs to change the owner. | [optional] [example: [1,2,3]] [nullable] |
| **fileIds** | [**List**](#model-batchrequestdtofileids) | The list of file IDs to change the owner. | [optional] [example: [1,2,3]] [nullable] |
| **userId** | **UUID** (uuid) | The new file owner ID. | [required] [example: 00000000-0000-0000-0000-000000000000] |


### Model ChatSettings
The chat settings.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **providerId** | **Integer** (int32) | The provider ID. | [optional] [example: 1] |
| **modelId** | **String** | The model ID. | [optional] [example: gpt-4] [nullable] |
| **prompt** | **String** | The prompt. | [optional] [example: Please analyze this document] [nullable] |
| **internal** | **Boolean** | Specifies whether the provider is internal or not. | [optional] [example: false] |


### Model ChatSettingsDto
The chat settings parameters.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **prompt** | **String** | The system prompt for the chat. | [optional] [example: You are a helpful assistant.] [nullable] |


### Model CheckConversionRequestDtoInteger
The parameters for checking file conversion.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **fileId** | **Integer** (int32) | The file ID to check conversion proccess. | [optional] [example: 1] |
| **sync** | **Boolean** | Specifies if the conversion process is synchronous or not. | [optional] [example: false] |
| **startConvert** | **Boolean** | Specifies whether to start a conversion process or not. | [optional] [example: true] |
| **version** | **Integer** (int32) | The file version that is converted. | [optional] [example: 1] |
| **password** | **String** | The password of the converted file. | [optional] [example: password123] [nullable] |
| **outputType** | **String** | The conversion output type. | [optional] [example: pdf] [nullable] |
| **createNewIfExist** | **Boolean** | Specifies whether to create a new file if it exists or not. | [optional] [example: false] |


### Model CheckDestFolderDto
The result of checking whether files can be moved or copied to the specified folder.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **result** | [**CheckDestFolderResult**](#model-checkdestfolderresult) |  | [optional] [enum: 0, 1, 2] |
| **files** | [**List**](#model-fileentrybasedto) | The list of files in the destination folder. | [optional] [example: [{"id":10,"title":"document.docx"}]] [nullable] |


### Model CheckDestFolderResult
[0 - All allowed, 1 - Part allowed, 2 - None allowed]

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|


### Model CheckDestFolderWrapper

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **response** | [**CheckDestFolderDto**](#model-checkdestfolderdto) |  | [optional] |
| **count** | **Integer** (int32) | The total number of items in the response | [optional] |
| **links** | [**List**](#model-booleanwrapperlinks-item) | List of links related to the response | [optional] |
| **status** | **Integer** (int32) | HTTP status code of the response | [optional] |
| **statusCode** | **Integer** (int32) | HTTP status code of the response (duplicate of status) | [optional] |


### Model CheckDocServiceUrlRequestDto
The request parameters for checking the document service location.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **docServiceUrl** | **String** | The ONLYOFFICE Docs URL address. | [required] [example: https://documentserver.example.com] [nullable] |
| **docServiceUrlInternal** | **String** | The ONLYOFFICE Docs URL address in the local private network. | [optional] [example: https://documentserver-internal.example.com] [nullable] |
| **docServiceUrlPortal** | **String** | The ONLYOFFICE Docs URL address. | [optional] [example: https://documentserver-portal.example.com] [nullable] |
| **docServiceSignatureSecret** | **String** | The signature secret of the ONLYOFFICE Docs. | [optional] [example: secret-key-123] [nullable] |
| **docServiceSignatureHeader** | **String** | The signature header of the ONLYOFFICE Docs. | [optional] [example: Authorization] [nullable] |
| **docServiceSslVerification** | **Boolean** | Specifies if the SSL verification of the ONLYOFFICE Docs is enabled or not. | [optional] [example: true] [nullable] |


### Model CheckFillFormDraft
The parameters for checking the form draft filling.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **version** | **Integer** (int32) | The file version of the form draft. | [required] [example: 1] |
| **action** | **String** | The action with the form draft. | [optional] [example: view] [nullable] |
| **requestView** | **Boolean** | Specifies whether to request the form for viewing or not. | [optional] [example: false] |
| **requestEmbedded** | **Boolean** | Specifies whether to request an embedded form or not. | [optional] [example: false] |


### Model CheckUploadRequest
The request parameters for checking file uploads.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **filesTitle** | **List** | The list of file titles. | [optional] [example: ["file1.docx","file2.pdf","file3.xlsx"]] [nullable] |


### Model ChunkedUploadSessionResponseInteger
Represents the response returned from a chunked upload session.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **id** | **String** | The unique identifier for the entity. | [optional] [example: 0af4bc0d-9a9d-450c-a72b-f14d9ac55c89] [nullable] |
| **path** | **List** (int32) | Represents the hierarchical path of folders associated with a chunked upload session. | [optional] [example: ["123","456","789"]] [nullable] |
| **created** | **Date** (date-time) | The timestamp indicating when the chunked upload session was created. | [optional] [example: 2024-01-15T10:30:00Z] |
| **expired** | **Date** (date-time) | The date and time when the chunked upload session is set to expire. | [optional] [example: 2024-01-15T11:30:00Z] |
| **location** | **String** | Represents the URI or path of the chunked upload session&#39;s current location. | [optional] [example: https://example.com/products/files/httphandlers/filehandler.ashx?action=upload] [nullable] |
| **bytes\_total** | **Long** (int64) | The total size, in bytes, of the file being uploaded in the chunked upload session. | [optional] [example: 10485760] |


### Model ChunkedUploadSessionResponseIntegerWrapper

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **response** | [**ChunkedUploadSessionResponseInteger**](#model-chunkeduploadsessionresponseinteger) |  | [optional] |
| **count** | **Integer** (int32) | The total number of items in the response | [optional] |
| **links** | [**List**](#model-booleanwrapperlinks-item) | List of links related to the response | [optional] |
| **status** | **Integer** (int32) | HTTP status code of the response | [optional] |
| **statusCode** | **Integer** (int32) | HTTP status code of the response (duplicate of status) | [optional] |


### Model ChunkedUploadSessionResponseWrapperInteger
Represents a wrapper for the response of a chunked upload session operation.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **success** | **Boolean** | Gets or sets a value indicating whether the operation was successful. | [optional] [example: true] |
| **data** | [**ChunkedUploadSessionResponseInteger**](#model-chunkeduploadsessionresponseinteger) |  | [optional] |


### Model ChunkedUploadSessionResponseWrapperIntegerWrapper

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **response** | [**ChunkedUploadSessionResponseWrapperInteger**](#model-chunkeduploadsessionresponsewrapperinteger) |  | [optional] |
| **count** | **Integer** (int32) | The total number of items in the response | [optional] |
| **links** | [**List**](#model-booleanwrapperlinks-item) | List of links related to the response | [optional] |
| **status** | **Integer** (int32) | HTTP status code of the response | [optional] |
| **statusCode** | **Integer** (int32) | HTTP status code of the response (duplicate of status) | [optional] |


### Model CoEditingConfig
The co-editing configuration parameters.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **change** | **Boolean** | Specifies if the co-editing mode can be changed in the editor interface or not. | [optional] [example: true] |
| **fast** | **Boolean** | Specifies if the co-editing mode is fast. | [optional] [example: false] |
| **mode** | [**CoEditingConfigMode**](#model-coeditingconfigmode) |  | [optional] [enum: 0, 1] |


### Model CoEditingConfigMode
[0 - Fast, 1 - Strict]

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|


### Model ConfigurationDtoInteger
The configuration parameters.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **document** | [**DocumentConfigDto**](#model-documentconfigdto) |  | [required] |
| **documentType** | **String** | The document type. | [required] [example: word] [nullable] |
| **editorConfig** | [**EditorConfigurationDto**](#model-editorconfigurationdto) |  | [required] |
| **editorType** | [**EditorType**](#model-editortype) |  | [required] [enum: 0, 1, 2] |
| **editorUrl** | **URI** (uri) | The editor URL. | [required] [example: http://localhost/editor] [nullable] |
| **token** | **String** | The token of the file configuration. | [optional] [example: token-abc-123] [nullable] |
| **type** | **String** | The platform type. | [optional] [example: desktop] [nullable] |
| **file** | [**FileDtoInteger**](#model-filedtointeger) |  | [required] |
| **errorMessage** | **String** | The error message. | [optional] [example: Configuration error] [nullable] |
| **startFilling** | **Boolean** | Specifies if the file filling has started or not. | [optional] [example: false] [nullable] |
| **fillingStatus** | **Boolean** | The file filling status. | [optional] [example: false] [nullable] |
| **startFillingMode** | [**StartFillingMode**](#model-startfillingmode) |  | [optional] [enum: 0, 1, 2, 3] |
| **fillingSessionId** | **String** | The file filling session ID. | [optional] [example: session-123-456] [nullable] |
| **quotaExceededScope** | [**QuotaScope**](#model-quotascope) |  | [optional] [enum: 0, 1, 2] |
| **generationToolCallState** | [**EditorToolCallStateDto**](#model-editortoolcallstatedto) |  | [optional] |


### Model ConfigurationIntegerWrapper

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **response** | [**ConfigurationDtoInteger**](#model-configurationdtointeger) |  | [optional] |
| **count** | **Integer** (int32) | The total number of items in the response | [optional] |
| **links** | [**List**](#model-booleanwrapperlinks-item) | List of links related to the response | [optional] |
| **status** | **Integer** (int32) | HTTP status code of the response | [optional] |
| **statusCode** | **Integer** (int32) | HTTP status code of the response (duplicate of status) | [optional] |


### Model Contact
The contact information.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **type** | **String** | The contact type. | [optional] [example: GTalk] [nullable] |
| **value** | **String** | The contact value. | [optional] [example: my@gmail.com] [nullable] |


### Model ConversationResultArrayWrapper

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **response** | [**List**](#model-conversationresultdto) |  | [optional] |
| **count** | **Integer** (int32) | The total number of items in the response | [optional] |
| **links** | [**List**](#model-booleanwrapperlinks-item) | List of links related to the response | [optional] |
| **status** | **Integer** (int32) | HTTP status code of the response | [optional] |
| **statusCode** | **Integer** (int32) | HTTP status code of the response (duplicate of status) | [optional] |


### Model ConversationResultDto
The result of file convertion operation.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **id** | **String** | The conversion operation ID. | [required] [example: 12345] [nullable] |
| **Operation** | [**FileOperationType**](#model-fileoperationtype) |  | [required] [enum: 0, 1, 2, 3, 4, 5, 6, 7] |
| **progress** | **Integer** (int32) | The conversion operation progress. | [required] [example: 50] |
| **source** | **String** | The source file for the conversion. | [optional] [example: document.docx] [nullable] |
| **result** | **oas_any_type_not_mapped** | The resulting file after the conversion. | [optional] [example: {"id":10,"title":"converted_file.pdf"}] [nullable] |
| **error** | **String** | The conversion operation error message. | [optional] [example: Conversion failed] [nullable] |
| **processed** | **String** | Specifies if the conversion operation is processed or not. | [optional] [example: true] [nullable] |


### Model CopyAsJsonElement
The parameters for copying a file.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **destTitle** | **String** | The copied file name. | [required] [example: Document Copy.docx] [nullable] |
| **destFolderId** | [**CopyAsJsonElement_destFolderId**](#model-copyasjsonelementdestfolderid) |  | [required] |
| **enableExternalExt** | **Boolean** | Specifies whether to allow creating the copied file of an external extension or not. | [optional] [example: false] |
| **password** | **String** | The copied file password. | [optional] [example: password123] [nullable] |
| **toForm** | **Boolean** | Specifies whether to convert the file to form or not. | [optional] [example: false] |


### Model CopyAsJsonElement.destFolderId
The destination folder ID of the copied file.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|


### Model CoverRequestDto
The request parameters to change the room cover.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **color** | **String** | The cover color. | [optional] [example: FF0000] [pattern: /^([A-Fa-f0-9]{6}\|[A-Fa-f0-9]{3})$/] [nullable] |
| **cover** | **String** | The cover name. | [optional] [example: cover1.jpg] [nullable] |


### Model CoversResultArrayWrapper

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **response** | [**List**](#model-coversresultdto) |  | [optional] |
| **count** | **Integer** (int32) | The total number of items in the response | [optional] |
| **links** | [**List**](#model-booleanwrapperlinks-item) | List of links related to the response | [optional] |
| **status** | **Integer** (int32) | HTTP status code of the response | [optional] |
| **statusCode** | **Integer** (int32) | HTTP status code of the response (duplicate of status) | [optional] |


### Model CoversResultDto
The result of the cover request containing the cover image data.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **id** | **String** | The cover unique identifier. | [required] [example: cover-123] [nullable] |
| **data** | **String** | The cover image data. | [required] [example: base64EncodedImageData] [nullable] |


### Model CreateFileJsonElement
The parameters for creating a file.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **title** | **String** | The file title for creation. | [required] [example: New Document.docx] [minLength: 0] [maxLength: 165] [nullable] |
| **templateId** | [**CreateFileJsonElement_templateId**](#model-createfilejsonelementtemplateid) |  | [optional] |
| **enableExternalExt** | **Boolean** | Specifies whether to allow creating a file of an external extension or not. | [optional] [example: false] |
| **formId** | **Integer** (int32) | The form ID for creation. | [optional] [example: 0] |


### Model CreateFileJsonElement.templateId
The template file ID for creation.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|


### Model CreateFolder
The parameters for creating a folder.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **title** | **String** | The folder title to create. | [required] [example: New Folder] [minLength: 0] [maxLength: 165] [nullable] |


### Model CreateRoomFromTemplateDto
The parameters for creating a room from a template.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **templateId** | **Integer** (int32) | The template ID from which the room to be created. | [required] [example: 1] |
| **title** | **String** | The room title. | [required] [example: My Room From Template] [nullable] |
| **logo** | [**LogoRequest**](#model-logorequest) |  | [optional] |
| **copyLogo** | **Boolean** | Specifies whether to copy a logo or not. | [optional] [example: false] |
| **tags** | **List** | The collection of tags. | [optional] [example: ["tag1","tag2","tag3"]] [nullable] |
| **color** | **String** | The color of the room to be created. | [optional] [example: #FF0000] [minLength: 0] [maxLength: 6] [nullable] |
| **cover** | **String** | The cover of the room to be created. | [optional] [example: cover1.jpg] [minLength: 0] [maxLength: 50] [nullable] |
| **quota** | **Long** (int64) | The room quota. | [optional] [example: 1073741824] [nullable] |
| **indexing** | **Boolean** | Specifies whether to create a room with indexing. | [optional] [example: true] [nullable] |
| **denyDownload** | **Boolean** | Specifies whether to deny downloads from the room. | [optional] [example: false] [nullable] |
| **lifetime** | [**RoomDataLifetimeDto**](#model-roomdatalifetimedto) |  | [optional] |
| **watermark** | [**WatermarkRequestDto**](#model-watermarkrequestdto) |  | [optional] |
| **private** | **Boolean** | Specifies whether the room to be created is private or not. | [optional] [example: false] [nullable] |


### Model CreateRoomRequestDto
The request parameters for creating a room.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **title** | **String** | The room name. | [required] [example: My Room] [minLength: 0] [maxLength: 170] [nullable] |
| **quota** | **Long** (int64) | The room quota. | [optional] [example: 1073741824] [nullable] |
| **indexing** | **Boolean** | Specifies whether to create a room with indexing. | [optional] [example: true] [nullable] |
| **denyDownload** | **Boolean** | Specifies whether to deny downloads from the room. | [optional] [example: false] [nullable] |
| **lifetime** | [**RoomDataLifetimeDto**](#model-roomdatalifetimedto) |  | [optional] |
| **watermark** | [**WatermarkRequestDto**](#model-watermarkrequestdto) |  | [optional] |
| **logo** | [**LogoRequest**](#model-logorequest) |  | [optional] |
| **tags** | **List** | The list of tags. | [optional] [example: ["tag1","tag2","tag3"]] [nullable] |
| **color** | **String** | The room color. | [optional] [example: #FF0000] [minLength: 0] [maxLength: 6] [nullable] |
| **cover** | **String** | The room cover. | [optional] [example: cover1.jpg] [minLength: 0] [maxLength: 50] [nullable] |
| **roomType** | [**RoomType**](#model-roomtype) |  | [required] [enum: 1, 2, 5, 6, 8, 9] |
| **private** | **Boolean** | Specifies whether the room to be created is private or not. | [optional] [example: false] |
| **share** | [**List**](#model-fileshareparams) | The collection of sharing parameters. | [optional] [example: [{"shareTo":"00000000-0000-0000-0000-000000000000","access":1}]] [nullable] |
| **chatSettings** | [**ChatSettings**](#model-chatsettings) |  | [optional] |
| **sendFormToExternalDB** | **Boolean** | Specifies whether to send form data to external database. | [optional] [example: false] [nullable] |
| **saveFormAsXLSX** | **Boolean** | Specifies whether to save form data as XLSX file. | [optional] [example: false] [nullable] |


### Model CreateTagRequestDto
The request parameters for creating a tag.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **name** | **String** | The tag name. | [required] [example: Important] [minLength: 0] [maxLength: 255] [nullable] |


### Model CreateTextOrHtmlFile
The parameters for creating an HTML or text file.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **title** | **String** | The file title for text or HTML file. | [required] [example: Document.txt] [minLength: 1] [maxLength: 165] [nullable] |
| **content** | **String** | The text or HTML file contents. | [optional] [example: This is the file content] [nullable] |
| **createNewIfExist** | **Boolean** | Specifies whether to create a new text or HTML file if it exists or not. | [optional] [example: false] |


### Model CreateThirdPartyRoom
The parameters for creating a third-party room.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **createAsNewFolder** | **Boolean** | Specifies whether to create a third-party room as a new folder or not. | [optional] [example: false] |
| **title** | **String** | The third-party room name to be created. | [required] [example: My Third-Party Room] [nullable] |
| **roomType** | [**RoomType**](#model-roomtype) |  | [required] [enum: 1, 2, 5, 6, 8, 9] |
| **private** | **Boolean** | Specifies whether to create the private third-party room or not. | [optional] [example: false] |
| **indexing** | **Boolean** | Specifies whether to create the third-party room with indexing. | [optional] [example: true] |
| **denyDownload** | **Boolean** | Specifies whether to deny downloads from the third-party room. | [optional] [example: false] |
| **color** | **String** | The color of the third-party room. | [optional] [example: #FF0000] [nullable] |
| **cover** | **String** | The cover of the third-party room. | [optional] [example: cover1.jpg] [nullable] |
| **tags** | **List** | The list of tags of the third-party room. | [optional] [example: ["tag1","tag2","tag3"]] [nullable] |
| **logo** | [**LogoRequest**](#model-logorequest) |  | [optional] |


### Model CustomFilterParameters
The parameters for setting the Custom Filter editing mode.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **enabled** | **Boolean** | Specifies whether the Custom Filter editing mode is enabled or not. | [optional] [example: true] |


### Model CustomerConfigDto
The customer config parameters.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **address** | **String** | The address of the customer configuration. | [optional] [example: 123 Main Street, City] [nullable] |
| **logo** | **String** | The logo of the customer configuration. | [optional] [example: http://localhost/customer-logo.png] [nullable] |
| **logoDark** | **String** | The dark logo of the customer configuration. | [optional] [example: http://localhost/customer-logo-dark.png] [nullable] |
| **mail** | **String** | The mail address of the customer configuration. | [optional] [example: contact@example.com] [nullable] |
| **name** | **String** | The name of the customer configuration. | [optional] [example: ONLYOFFICE] [nullable] |
| **www** | **String** | The site web address of the customer configuration. | [optional] [example: https://www.example.com] [nullable] |


### Model CustomizationConfigDto
The customization config parameters.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **about** | **Boolean** | Specifies if the customization is about. | [optional] [example: true] |
| **customer** | [**CustomerConfigDto**](#model-customerconfigdto) |  | [optional] |
| **anonymous** | [**AnonymousConfigDto**](#model-anonymousconfigdto) |  | [optional] |
| **feedback** | [**FeedbackConfig**](#model-feedbackconfig) |  | [optional] |
| **forcesave** | **Boolean** | Specifies if the customization should be force saved. | [optional] [example: false] [nullable] |
| **goback** | [**GobackConfig**](#model-gobackconfig) |  | [optional] |
| **review** | [**ReviewConfig**](#model-reviewconfig) |  | [optional] |
| **logo** | [**LogoConfigDto**](#model-logoconfigdto) |  | [optional] |
| **mentionShare** | **Boolean** | Specifies if the share should be mentioned. | [optional] [example: true] |
| **submitForm** | [**SubmitForm**](#model-submitform) |  | [optional] |
| **startFillingForm** | [**StartFillingForm**](#model-startfillingform) |  | [optional] |


### Model DarkThemeSettingsType
[Base - Base, Dark - Dark, System - System]

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|


### Model DateToAutoCleanUp
[1 - One week, 2 - Two weeks, 3 - One month, 4 - Thirty days, 5 - Two months, 6 - Three months]

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|


### Model DefaultTemplateItemDto
Default template setting

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **selectedFile** | **Integer** (int32) | File id to use as a default template | [optional] [example: 123] [nullable] |
| **fileExtension** | **String** | Extension of a default template | [required] [example: .docx] [nullable] |
| **fileTitle** | **String** | Title of a default template | [optional] [example: Default Template] [nullable] |
| **lastModified** | **Date** (date-time) | Last modified date of a default template | [optional] [nullable] |
| **fileSize** | **Long** (int64) | Filesize (in bytes) of a default template | [optional] [example: 1024] [nullable] |
| **viewUrl** | **String** | View url of a default template | [optional] [example: http://localhost/template/view] [nullable] |


### Model DefaultTemplateSettingsDto
Default templates settings parameters.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **items** | [**List**](#model-defaulttemplateitemdto) | Default templates list. | [required] [example: [{"extension":".docx","title":"Blank Document"}]] [nullable] |


### Model DefaultTemplateSettingsRequestDto
Default templates settings request parameters.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **selectedFile** | [**DefaultTemplateSettingsRequestDto_selectedFile**](#model-defaulttemplatesettingsrequestdtoselectedfile) |  | [required] |
| **fileExtension** | **String** | File extension of a template to replace | [required] [example: .docx] [nullable] |


### Model DefaultTemplateSettingsRequestDto.selectedFile
File id to replace template with

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|


### Model DefaultTemplateSettingsResetRequestDto
Default templates settings reset request parameters.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **fileExtension** | **String** | File extension of a template to reset | [required] [example: .docx] [nullable] |


### Model DefaultTemplateSettingsWrapper

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **response** | [**DefaultTemplateSettingsDto**](#model-defaulttemplatesettingsdto) |  | [optional] |
| **count** | **Integer** (int32) | The total number of items in the response | [optional] |
| **links** | [**List**](#model-booleanwrapperlinks-item) | List of links related to the response | [optional] |
| **status** | **Integer** (int32) | HTTP status code of the response | [optional] |
| **statusCode** | **Integer** (int32) | HTTP status code of the response (duplicate of status) | [optional] |


### Model Delete
The parameters for deleting a file.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **deleteAfter** | **Boolean** | Specifies whether to delete a file after the editing session is finished or not. | [optional] [example: false] |
| **immediately** | **Boolean** | Specifies whether to move a file to the \\Trash\\ folder or delete it immediately. | [optional] [example: false] |


### Model DeleteBatchRequestDto
The request parameters for deleting files.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **returnSingleOperation** | **Boolean** | Specifies whether to return only the current operation | [optional] |
| **folderIds** | [**List**](#model-deletebatchrequestdtofolderids) | The list of folder IDs to be deleted. | [optional] [nullable] |
| **fileIds** | [**List**](#model-deletebatchrequestdtofileids) | The list of file IDs to be deleted. | [optional] [nullable] |
| **deleteAfter** | **Boolean** | Specifies whether to delete a file after the editing session is finished or not | [optional] |
| **immediately** | **Boolean** | Specifies whether to move a file to the \\Trash\\ folder or delete it immediately. | [optional] |


### Model DeleteBatchRequestDto.fileIds

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|


### Model DeleteBatchRequestDto.folderIds

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|


### Model DeleteFolder
The parameters for deleting a folder.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **deleteAfter** | **Boolean** | Specifies whether to delete a folder after the editing session is finished or not. | [optional] [example: false] |
| **immediately** | **Boolean** | Specifies whether to move a folder to the \\Trash\\ folder or delete it immediately. | [optional] [example: false] |


### Model DeleteRoomRequest
The parameters for deleting a room.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **deleteAfter** | **Boolean** | Specifies whether to delete a room after the editing session is finished or not. | [optional] [example: false] |


### Model DeleteVersionBatchRequestDto
The request parameters for deleting file versions.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **returnSingleOperation** | **Boolean** | Specifies whether to return only the current operation | [optional] |
| **deleteAfter** | **Boolean** | Specifies whether to delete a file after the editing session is finished or not. | [optional] |
| **fileId** | **Integer** (int32) | The file ID to delete. | [required] |
| **versions** | **List** (int32) | The collection of file versions to be deleted. | [required] [nullable] |


### Model DisplayRequestDto
The settings request parameters.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **set** | **Boolean** | Specifies whether to set the specified settings or not. | [optional] [example: true] |


### Model DistributedTaskStatus
[0 - Created, 1 - Running, 2 - Completed, 3 - Canceled, 4 - Failted]

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|


### Model DocServiceUrlDto
The document service URL parameters.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **version** | **String** | The version of the document service. | [required] [example: 8.0.1] [nullable] |
| **docServiceUrlApi** | **String** | The document service URL API. | [required] [example: http://localhost/api] [nullable] |
| **docServiceUrl** | **String** | The document service URL. | [required] [example: http://localhost/docservice] [nullable] |
| **docServicePreloadUrl** | **String** | The URL used to preload the document service scripts. | [required] [example: http://localhost/preload] [nullable] |
| **docServiceUrlInternal** | **String** | The internal document service URL. | [required] [example: http://localhost/internal] [nullable] |
| **docServicePortalUrl** | **String** | The document service portal URL. | [required] [example: http://localhost/portal] [nullable] |
| **docServiceSignatureHeader** | **String** | The document service signature header. | [required] [example: Authorization] [nullable] |
| **docServiceSslVerification** | **Boolean** | Specifies if the document service SSL verification is enabled. | [required] [example: true] |
| **isDefault** | **Boolean** | Specifies if the document service is default. | [required] [example: true] |


### Model DocServiceUrlWrapper

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **response** | [**DocServiceUrlDto**](#model-docserviceurldto) |  | [optional] |
| **count** | **Integer** (int32) | The total number of items in the response | [optional] |
| **links** | [**List**](#model-booleanwrapperlinks-item) | List of links related to the response | [optional] |
| **status** | **Integer** (int32) | HTTP status code of the response | [optional] |
| **statusCode** | **Integer** (int32) | HTTP status code of the response (duplicate of status) | [optional] |


### Model DocumentBuilderTaskDto
The Document Builder task parameters.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **id** | **String** | The Document Builder task ID. | [required] [example: task-123-456] [nullable] |
| **error** | **String** | The error message occurred during the document building process. | [required] [example: Build failed] [nullable] |
| **percentage** | **Integer** (int32) | The progress percentage of the document building process. | [required] [example: 75] |
| **isCompleted** | **Boolean** | Specifies whether the document building process is completed or not. | [required] [example: false] |
| **status** | [**DistributedTaskStatus**](#model-distributedtaskstatus) |  | [required] [enum: 0, 1, 2, 3, 4] |
| **resultFileId** | **oas_any_type_not_mapped** | The result file ID. | [required] [example: 123] [nullable] |
| **resultFileName** | **String** | The result file name. | [required] [example: result.docx] [nullable] |
| **resultFileUrl** | **String** | The result file URL. | [required] [example: http://localhost/files/result.docx] [nullable] |


### Model DocumentBuilderTaskWrapper

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **response** | [**DocumentBuilderTaskDto**](#model-documentbuildertaskdto) |  | [optional] |
| **count** | **Integer** (int32) | The total number of items in the response | [optional] |
| **links** | [**List**](#model-booleanwrapperlinks-item) | List of links related to the response | [optional] |
| **status** | **Integer** (int32) | HTTP status code of the response | [optional] |
| **statusCode** | **Integer** (int32) | HTTP status code of the response (duplicate of status) | [optional] |


### Model DocumentConfigDto
The document config parameters.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **fileType** | **String** | The file type of the document. | [optional] [example: docx] [nullable] |
| **info** | [**InfoConfigDto**](#model-infoconfigdto) |  | [optional] |
| **isLinkedForMe** | **Boolean** | Specifies if the documnet is linked for current user. | [optional] [example: false] |
| **key** | **String** | The document key. | [optional] [example: doc-key-123-abc] [nullable] |
| **permissions** | [**PermissionsConfig**](#model-permissionsconfig) |  | [optional] |
| **sharedLinkParam** | **String** | The shared link parameter of the document. | [optional] [example: share-param-123] [nullable] |
| **sharedLinkKey** | **String** | The shared link key of the document. | [optional] [example: share-key-abc] [nullable] |
| **referenceData** | [**FileReferenceData**](#model-filereferencedata) |  | [optional] |
| **title** | **String** | The document title. | [optional] [example: Document Title] [nullable] |
| **url** | **URI** (uri) | The document url. | [optional] [example: http://localhost/documents/doc.docx] [nullable] |
| **isForm** | **Boolean** | Indicates whether this is a form. | [optional] [example: false] |
| **options** | [**Options**](#model-options) |  | [optional] |


### Model DownloadRequestDto
The request parameters for downloading files.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **returnSingleOperation** | **Boolean** | Specifies whether to return only the current operation | [optional] |
| **folderIds** | [**List**](#model-downloadrequestdtofolderids) | The list of folder IDs to be downloaded. | [optional] [nullable] |
| **fileIds** | [**List**](#model-downloadrequestdtofileids) | The list of file IDs to be downloaded. | [optional] [nullable] |
| **fileConvertIds** | [**List**](#model-downloadrequestitemdto) | The list of file IDs which will be converted. | [optional] [nullable] |


### Model DownloadRequestDto.fileIds

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|


### Model DownloadRequestDto.folderIds

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|


### Model DownloadRequestItemDto
The download request item with conversion parameters and security settings.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **key** | [**DownloadRequestItemDto_key**](#model-downloadrequestitemdtokey) |  | [required] |
| **value** | **String** | The target format or conversion type for the file download. | [required] [example: pdf] [nullable] |
| **password** | **String** | The optional password for accessing protected files. | [optional] [example: password123] [nullable] |


### Model DownloadRequestItemDto.key
The unique identifier or reference key for the file to be downloaded.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|


### Model DraftLocationInteger
The file draft parameters.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **folderId** | **Integer** (int32) | The InProcess folder ID of the draft. | [optional] [example: 10] |
| **folderTitle** | **String** | The InProcess folder title of the draft. | [optional] [example: Draft Folder] [nullable] |
| **fileId** | **Integer** (int32) | The draft ID. | [optional] [example: 123] |
| **fileTitle** | **String** | The draft title. | [optional] [example: Draft Document] [nullable] |


### Model DuplicateRequestDto
The request parameters for duplicating files and fodlers.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **returnSingleOperation** | **Boolean** | Specifies whether to return only the current operation | [optional] |
| **folderIds** | [**List**](#model-duplicaterequestdtofolderids) | The list of folder IDs. | [optional] [nullable] |
| **fileIds** | [**List**](#model-duplicaterequestdtofileids) | The list of file IDs. | [optional] [nullable] |


### Model DuplicateRequestDto.fileIds

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|


### Model DuplicateRequestDto.folderIds

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|


### Model EditHistoryArrayWrapper

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **response** | [**List**](#model-edithistorydto) |  | [optional] |
| **count** | **Integer** (int32) | The total number of items in the response | [optional] |
| **links** | [**List**](#model-booleanwrapperlinks-item) | List of links related to the response | [optional] |
| **status** | **Integer** (int32) | HTTP status code of the response | [optional] |
| **statusCode** | **Integer** (int32) | HTTP status code of the response (duplicate of status) | [optional] |


### Model EditHistoryAuthor
The information about the file editing history author.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **id** | **String** | The author ID. | [required] [example: author_123] [nullable] |
| **name** | **String** | The author name. | [optional] [example: John Doe] [nullable] |


### Model EditHistoryChangesWrapper
The parameters of the file editing history.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **user** | [**EditHistoryAuthor**](#model-edithistoryauthor) |  | [optional] |
| **created** | [**ApiDateTime**](#model-apidatetime) |  | [optional] |
| **documentSha256** | **String** | The document hash generated by the SHA-256 algorithm. | [optional] [example: a1b2c3d4e5f6g7h8i9j0] [nullable] |


### Model EditHistoryDataDto
The file editing history data.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **changesUrl** | **URI** (uri) | The URL address of the file with the document changes data. | [optional] [example: https://example.com/changes] [nullable] |
| **key** | **String** | The document identifier used to unambiguously identify the document file. | [required] [example: doc1] [nullable] |
| **previous** | [**EditHistoryUrl**](#model-edithistoryurl) |  | [optional] |
| **token** | **String** | The encrypted signature added to the parameter in the form of a token. | [optional] [example: token] [nullable] |
| **url** | **URI** (uri) | The URL address of the current document version. | [required] [example: https://example.com/file.docx] [nullable] |
| **version** | **Integer** (int32) | The document version number. | [required] [example: 1] |
| **fileType** | **String** | The document extension. | [required] [example: docx] [nullable] |


### Model EditHistoryDataWrapper

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **response** | [**EditHistoryDataDto**](#model-edithistorydatadto) |  | [optional] |
| **count** | **Integer** (int32) | The total number of items in the response | [optional] |
| **links** | [**List**](#model-booleanwrapperlinks-item) | List of links related to the response | [optional] |
| **status** | **Integer** (int32) | HTTP status code of the response | [optional] |
| **statusCode** | **Integer** (int32) | HTTP status code of the response (duplicate of status) | [optional] |


### Model EditHistoryDto
The file editing history parameters.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **id** | **Integer** (int32) | The document ID. | [optional] [example: 123] |
| **key** | **String** | The document identifier used to unambiguously identify the document file. | [optional] [example: doc-key-abc123] [nullable] |
| **version** | **Integer** (int32) | The document version number. | [optional] [example: 2] |
| **versionGroup** | **Integer** (int32) | The document version group. | [optional] [example: 1] |
| **user** | [**EditHistoryAuthor**](#model-edithistoryauthor) |  | [optional] |
| **created** | [**ApiDateTime**](#model-apidatetime) |  | [optional] |
| **changesHistory** | **String** | The file history changes in the string format. | [optional] [example: Changes history text] [nullable] |
| **changes** | [**List**](#model-edithistorychangeswrapper) | The list of file history changes. | [optional] [example: [{"user":{"id":"123","name":"John Doe"},"created":"2021-01-01T00:00:00Z"}]] [nullable] |
| **serverVersion** | **String** | The current server version number. | [optional] [example: 8.0.1] [nullable] |


### Model EditHistoryUrl
The file editing history URL parameters.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **key** | **String** | The document identifier of the previous version of the document. | [optional] [example: doc_v2_20260101] [nullable] |
| **url** | **URI** (uri) | The url address of the previous version of the document. | [optional] [example: https://files.example.com/history/doc_v2_20260101.docx] [nullable] |
| **fileType** | **String** | The document extension. | [optional] [example: .docx] [nullable] |


### Model EditorConfigurationDto
The editor configuration parameters.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **callbackUrl** | **URI** (uri) | The callback URL of the editor. | [optional] [example: http://localhost/callback] [nullable] |
| **coEditing** | [**CoEditingConfig**](#model-coeditingconfig) |  | [optional] |
| **createUrl** | **String** | The creation URL of the editor. | [optional] [example: http://localhost/create] [nullable] |
| **customization** | [**CustomizationConfigDto**](#model-customizationconfigdto) |  | [optional] |
| **embedded** | [**EmbeddedConfig**](#model-embeddedconfig) |  | [optional] |
| **encryptionKeys** | [**List**](#model-encryptionkeydto) | The encryption keys of the editor configuration. | [optional] [nullable] |
| **lang** | **String** | The language of the editor configuration. | [required] [example: en-US] [nullable] |
| **mode** | **String** | The mode of the editor configuration. | [required] [example: edit] [nullable] |
| **modeWrite** | **Boolean** | Specifies if the mode is write of the editor configuration. | [optional] [example: true] |
| **plugins** | [**PluginsConfig**](#model-pluginsconfig) |  | [optional] |
| **recent** | [**List**](#model-recentconfig) | The recent configuration of the editor. | [optional] [example: []] [nullable] |
| **templates** | [**List**](#model-templatesconfig) | The templates of the editor configuration. | [optional] [example: []] [nullable] |
| **user** | [**UserConfig**](#model-userconfig) |  | [optional] |


### Model EditorToolCallStateDto
The editor tool call state. Used to run the agent flow in the editor.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **toolName** | **String** | The tool name. | [required] [example: GenerateDocx] [nullable] |
| **parameters** | **Object** | The editor tool call parameters. | [required] |


### Model EditorType
[0 - Desktop, 1 - Mobile, 2 - Embedded]

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|


### Model EmailInvitationDto
The email invitation parameters.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **email** | **String** (email) | The email address. | [optional] [example: user@example.com] [maxLength: 255] [nullable] |


### Model EmbeddedConfig
The configuration parameters for the embedded document type.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **embedUrl** | **String** | The absolute URL to the document serving as a source file for the document embedded into the web page. | [optional] [example: https://portal.example.com/files/editor?action=embedded&share=abc123] [nullable] |
| **saveUrl** | **String** | The absolute URL that will allow the document to be saved onto the user personal computer. | [optional] [example: https://portal.example.com/files/filehandler?action=download&share=abc123] [nullable] |
| **shareLinkParam** | **String** | The shared URL parameter. | [optional] [example: &share=abc123] [nullable] |
| **shareUrl** | **String** | The absolute URL that will allow other users to share this document. | [optional] [example: https://portal.example.com/files/editor?action=view&share=abc123] [nullable] |
| **toolbarDocked** | **String** | The place for the embedded viewer toolbar, can be either top or bottom. | [optional] [example: top] [nullable] |


### Model EmployeeActivationStatus
[0 - Not activated, 1 - Activated, 2 - Pending, 4 - Auto generated]

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|


### Model EmployeeDto
The user parameters.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **id** | **UUID** (uuid) | The user ID. | [optional] [example: 00000000-0000-0000-0000-000000000000] |
| **displayName** | **String** | The HTML-encoded user&#39;s display name formatted according to the default format for the current culture. | [optional] [example: Mike Zanyatski] [nullable] |
| **avatar** | **String** | The user avatar. | [optional] [example: https://example.com/avatar.jpg] [nullable] |
| **avatarOriginal** | **String** | The user original size avatar. | [optional] [example: https://example.com/avatar_original.jpg] [nullable] |
| **avatarMax** | **String** | The user maximum size avatar. | [optional] [example: https://example.com/avatar_max.jpg] [nullable] |
| **avatarMedium** | **String** | The user medium size avatar. | [optional] [example: https://example.com/avatar_medium.jpg] [nullable] |
| **avatarSmall** | **String** | The user small size avatar. | [optional] [example: https://example.com/avatar_small.jpg] [nullable] |
| **profileUrl** | **String** | The user profile URL. | [optional] [example: https://example.com/profile/user123] [nullable] |
| **hasAvatar** | **Boolean** | Specifies if the user has an avatar or not. | [optional] [example: true] |
| **isAnonim** | **Boolean** | Specifies if the user is anonymous or not. | [optional] [example: false] |


### Model EmployeeFullDto
The full list of user parameters.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **id** | **UUID** (uuid) | The user ID. | [optional] |
| **displayName** | **String** | The HTML-encoded user&#39;s display name formatted according to the default format for the current culture. | [optional] [nullable] |
| **avatar** | **String** | The user avatar. | [optional] [nullable] |
| **avatarOriginal** | **String** | The user original size avatar. | [optional] [nullable] |
| **avatarMax** | **String** | The user maximum size avatar. | [optional] [nullable] |
| **avatarMedium** | **String** | The user medium size avatar. | [optional] [nullable] |
| **avatarSmall** | **String** | The user small size avatar. | [optional] [nullable] |
| **profileUrl** | **String** | The user profile URL. | [optional] [nullable] |
| **hasAvatar** | **Boolean** | Specifies if the user has an avatar or not. | [optional] |
| **isAnonim** | **Boolean** | Specifies if the user is anonymous or not. | [optional] |
| **firstName** | **String** | The user first name. | [optional] [nullable] |
| **lastName** | **String** | The user last name. | [optional] [nullable] |
| **userName** | **String** | The user username. | [optional] [nullable] |
| **email** | **String** (email) | The user email. | [optional] [nullable] |
| **contacts** | [**List**](#model-contact) | The list of user contacts. | [optional] [nullable] |
| **status** | [**EmployeeStatus**](#model-employeestatus) |  | [optional] [enum: 1, 2, 4, 5, 7] |
| **activationStatus** | [**EmployeeActivationStatus**](#model-employeeactivationstatus) |  | [optional] [enum: 0, 1, 2, 4] |
| **terminated** | [**ApiDateTime**](#model-apidatetime) |  | [optional] |
| **department** | **String** | The user department. | [optional] [nullable] |
| **groups** | [**List**](#model-groupsummarydto) | The list of user groups. | [optional] [nullable] |
| **location** | **String** | The user location. | [optional] [nullable] |
| **notes** | **String** | The user notes. | [optional] [nullable] |
| **isAdmin** | **Boolean** | Specifies if the user is an administrator or not. | [optional] |
| **isRoomAdmin** | **Boolean** | Specifies if the user is a room administrator or not. | [optional] |
| **isLDAP** | **Boolean** | Specifies if the LDAP settings are enabled for the user or not. | [optional] |
| **listAdminModules** | **List** | The list of the administrator modules. | [optional] [nullable] |
| **isOwner** | **Boolean** | Specifies if the user is a portal owner or not. | [optional] |
| **isVisitor** | **Boolean** | Specifies if the user is a portal visitor or not. | [optional] |
| **isCollaborator** | **Boolean** | Specifies if the user is a portal collaborator or not. | [optional] |
| **cultureName** | **String** | The user culture code. | [optional] [nullable] |
| **mobilePhone** | **String** | The user mobile phone number. | [optional] [nullable] |
| **mobilePhoneActivationStatus** | [**MobilePhoneActivationStatus**](#model-mobilephoneactivationstatus) |  | [optional] [enum: 0, 1] |
| **isSSO** | **Boolean** | Specifies if the SSO settings are enabled for the user or not. | [optional] |
| **theme** | [**DarkThemeSettingsType**](#model-darkthemesettingstype) |  | [optional] [enum: Base, Dark, System] |
| **quotaLimit** | **Long** (int64) | The user quota limit. | [optional] [nullable] |
| **usedSpace** | **Double** (double) | The portal used space of the user. | [optional] [nullable] |
| **shared** | **Boolean** | Specifies if the user has access rights. | [optional] [nullable] |
| **isCustomQuota** | **Boolean** | Specifies if the user has a custom quota or not. | [optional] [nullable] |
| **loginEventId** | **Integer** (int32) | The current login event ID. | [optional] [nullable] |
| **authCookieLifetime** | **Double** (double) | The auth cookie lifetime in seconds. | [optional] [nullable] |
| **createdBy** | [**EmployeeDto**](#model-employeedto) |  | [optional] |
| **registrationDate** | [**ApiDateTime**](#model-apidatetime) |  | [optional] |
| **hasPersonalFolder** | **Boolean** | Specifies if the user has a personal folder or not. | [optional] [nullable] |
| **tfaAppEnabled** | **Boolean** | Indicates whether the user has enabled two-factor authentication (TFA) using an authentication app. | [optional] [nullable] |


### Model EmployeeStatus
[1 - Active, 2 - Terminated, 4 - Pending, 5 - Default, 7 - All]

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|


### Model EncryptionKeyArrayWrapper

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **response** | [**List**](#model-encryptionkeydto) |  | [optional] |
| **count** | **Integer** (int32) | The total number of items in the response | [optional] |
| **links** | [**List**](#model-booleanwrapperlinks-item) | List of links related to the response | [optional] |
| **status** | **Integer** (int32) | HTTP status code of the response | [optional] |
| **statusCode** | **Integer** (int32) | HTTP status code of the response (duplicate of status) | [optional] |


### Model EncryptionKeyDto

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **id** | **UUID** (uuid) |  | [optional] |
| **userId** | **UUID** (uuid) |  | [optional] |
| **date** | **Date** (date-time) |  | [optional] |
| **publicKey** | **String** |  | [optional] [nullable] |
| **privateKeyEnc** | **String** |  | [optional] [nullable] |
| **cryptoEngineId** | **String** |  | [optional] [nullable] |


### Model EncryptionKeyRequestDto

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **id** | **UUID** (uuid) |  | [optional] |
| **publicKey** | **String** |  | [optional] [nullable] |
| **privateKeyEnc** | **String** |  | [optional] [nullable] |


### Model ExternalDbSyncFormResultDto
The result of an external DB synchronization for a single form.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **id** | **Integer** (int32) | The form file ID. | [optional] [example: 42] |
| **title** | **String** | The form file title. | [optional] [example: Application.pdf] [nullable] |
| **success** | **Boolean** | Specifies whether the synchronization succeeded for this form. | [optional] [example: true] |
| **error** | **String** | The error message if the synchronization failed for this form. | [optional] [example: Connection refused] [nullable] |


### Model ExternalDbSyncTaskDto
The external DB synchronization task parameters.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **id** | **String** | The task ID. | [required] [example: ExternalDbSyncTask_1_42] [nullable] |
| **error** | **String** | The error message if the synchronization failed. | [optional] [example: Connection refused] [nullable] |
| **percentage** | **Integer** (int32) | The progress percentage of the synchronization. | [required] [example: 75] |
| **isCompleted** | **Boolean** | Specifies whether the synchronization is completed or not. | [required] [example: false] |
| **status** | [**DistributedTaskStatus**](#model-distributedtaskstatus) |  | [required] [enum: 0, 1, 2, 3, 4] |
| **forms** | [**List**](#model-externaldbsyncformresultdto) | The synchronization results for all original forms in the room. | [required] [example: [{"id":42,"title":"Application.pdf","success":true,"error":null}]] [nullable] |


### Model ExternalDbSyncTaskWrapper

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **response** | [**ExternalDbSyncTaskDto**](#model-externaldbsynctaskdto) |  | [optional] |
| **count** | **Integer** (int32) | The total number of items in the response | [optional] |
| **links** | [**List**](#model-booleanwrapperlinks-item) | List of links related to the response | [optional] |
| **status** | **Integer** (int32) | HTTP status code of the response | [optional] |
| **statusCode** | **Integer** (int32) | HTTP status code of the response (duplicate of status) | [optional] |


### Model ExternalShareDto
The external sharing information and validation data.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **status** | [**Status**](#model-status) |  | [required] [enum: 0, 1, 2, 3, 4, 5] |
| **id** | **String** | The external data ID. | [optional] [example: 123] [nullable] |
| **title** | **String** | The external data title. | [optional] [example: Shared Document] [nullable] |
| **type** | [**FileEntryType**](#model-fileentrytype) |  | [optional] [enum: 1, 2] |
| **tenantId** | **Integer** (int32) | The tenant ID. | [required] [example: 1] |
| **entityId** | **String** | The unique identifier of the shared entity. | [optional] [example: 456] [nullable] |
| **entityTitle** | **String** | The title of the shared entity. | [optional] [example: Entity Title] [nullable] |
| **entityType** | [**FileEntryType**](#model-fileentrytype) |  | [optional] [enum: 1, 2] |
| **isRoom** | **Boolean** | Indicates whether the entity represents a room. | [optional] [example: false] [nullable] |
| **shared** | **Boolean** | Specifies whether to share the external data or not. | [required] [example: true] |
| **linkId** | **UUID** (uuid) | The link ID of the external data. | [required] [example: 00000000-0000-0000-0000-000000000000] |
| **isAuthenticated** | **Boolean** | Specifies whether the user is authenticated or not. | [required] [example: true] |
| **isRoomMember** | **Boolean** | The room ID of the external data. | [optional] [example: false] |


### Model ExternalShareRequestParam
The external data parameters.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **password** | **String** | The password to share external data. | [optional] [example: p@ssw0rd] [nullable] |


### Model ExternalShareWrapper

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **response** | [**ExternalShareDto**](#model-externalsharedto) |  | [optional] |
| **count** | **Integer** (int32) | The total number of items in the response | [optional] |
| **links** | [**List**](#model-booleanwrapperlinks-item) | List of links related to the response | [optional] |
| **status** | **Integer** (int32) | HTTP status code of the response | [optional] |
| **statusCode** | **Integer** (int32) | HTTP status code of the response (duplicate of status) | [optional] |


### Model ExternalSharingSettingsDto
The Access Control external sharing settings.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **externalShare** | **Boolean** | Specifies whether external (public) link creation is allowed. | [optional] [example: true] |
| **defaultShareLinkInternal** | **Boolean** | Specifies the default sharing link type: true &#x3D; DocSpace users only, false &#x3D; Anyone with the link. | [optional] [example: false] |
| **externalShareApplyToDocuments** | **Boolean** | When external sharing is restricted, specifies whether the restriction applies to the My Documents section. | [optional] [example: true] |
| **externalShareApplyToRooms** | **Boolean** | When external sharing is restricted, specifies whether the restriction applies to the Rooms section. | [optional] [example: true] |
| **blockExistingLinksOnRestrict** | **Boolean** | When external sharing is restricted, specifies whether existing public links are blocked immediately. | [optional] [example: true] |


### Model ExternalSharingSettingsRequestDto
The Access Control external sharing settings request parameters.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **externalShare** | **Boolean** | Specifies whether external (public) link creation is allowed. | [optional] [example: true] |
| **defaultShareLinkInternal** | **Boolean** | Specifies the default sharing link type: true &#x3D; DocSpace users only, false &#x3D; Anyone with the link.  Relevant only when ExternalShare is true. | [optional] [example: false] |
| **externalShareApplyToDocuments** | **Boolean** | When external sharing is restricted, specifies whether to apply the restriction to the My Documents section.  Relevant only when ExternalShare is false. | [optional] [example: true] |
| **externalShareApplyToRooms** | **Boolean** | When external sharing is restricted, specifies whether to apply the restriction to the Rooms section.  Relevant only when ExternalShare is false. | [optional] [example: true] |
| **blockExistingLinksOnRestrict** | **Boolean** | When external sharing is restricted, specifies whether to block existing public links immediately.  Relevant only when ExternalShare is false. | [optional] [example: true] |


### Model ExternalSharingSettingsWrapper

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **response** | [**ExternalSharingSettingsDto**](#model-externalsharingsettingsdto) |  | [optional] |
| **count** | **Integer** (int32) | The total number of items in the response | [optional] |
| **links** | [**List**](#model-booleanwrapperlinks-item) | List of links related to the response | [optional] |
| **status** | **Integer** (int32) | HTTP status code of the response | [optional] |
| **statusCode** | **Integer** (int32) | HTTP status code of the response (duplicate of status) | [optional] |


### Model FeedbackConfig
The settings for the Feedback &amp; Support menu button.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **url** | **String** | The absolute URL to the website address which will be opened when clicking the Feedback &amp; Support menu button. | [optional] [example: https://portal.example.com/support] [nullable] |
| **visible** | **Boolean** | Shows or hides the Feedback &amp; Support menu button. | [optional] [example: true] |


### Model FileConflictResolveType
[Skip - Skip, Overwrite - Overwrite, Duplicate - Duplicate]

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|


### Model FileDtoInteger
The file parameters.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **title** | **String** | The file entry title. | [optional] [nullable] |
| **access** | [**FileShare**](#model-fileshare) |  | [optional] [enum: 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11] |
| **sharedBy** | [**EmployeeDto**](#model-employeedto) |  | [optional] |
| **ownedBy** | [**EmployeeDto**](#model-employeedto) |  | [optional] |
| **shared** | **Boolean** | Specifies if the file entry is shared via link or not. | [optional] |
| **sharedForUser** | **Boolean** | Specifies if the file entry is shared for user or not. | [optional] |
| **sharedExternal** | **Boolean** | Specifies if the file entry is shared via a public (non-internal) external link. | [optional] |
| **parentShared** | **Boolean** | Indicates whether the parent entity is shared. | [optional] |
| **shortWebUrl** | **URI** (uri) | The short Web URL. | [optional] [nullable] |
| **created** | [**ApiDateTime**](#model-apidatetime) |  | [optional] |
| **createdBy** | [**EmployeeDto**](#model-employeedto) |  | [optional] |
| **updated** | [**ApiDateTime**](#model-apidatetime) |  | [optional] |
| **autoDelete** | [**ApiDateTime**](#model-apidatetime) |  | [optional] |
| **rootFolderType** | [**FolderType**](#model-foldertype) |  | [optional] [enum: 0, 1, 2, 3, 5, 6, 8, 10, 11, 12, 13, 14, 15, 16, 19, 20, 21, 22, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36] |
| **parentRoomType** | [**FolderType**](#model-foldertype) |  | [optional] [enum: 0, 1, 2, 3, 5, 6, 8, 10, 11, 12, 13, 14, 15, 16, 19, 20, 21, 22, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36] |
| **updatedBy** | [**EmployeeDto**](#model-employeedto) |  | [optional] |
| **providerItem** | **Boolean** | Specifies if the file entry provider is specified or not. | [optional] [nullable] |
| **providerKey** | **String** | The provider key of the file entry. | [optional] [nullable] |
| **providerId** | **Integer** (int32) | The provider ID of the file entry. | [optional] [nullable] |
| **order** | **String** | The order of the file entry. | [optional] [nullable] |
| **isFavorite** | **Boolean** | Specifies if the file is a favorite or not. | [optional] [nullable] |
| **fileEntryType** | [**FileEntryType**](#model-fileentrytype) |  | [optional] [enum: 1, 2] |
| **id** | **Integer** (int32) | The file entry ID. | [optional] |
| **rootFolderId** | **Integer** (int32) | The root folder ID of the file entry. | [optional] |
| **originId** | **Integer** (int32) | The origin ID of the file entry. | [optional] |
| **originRoomId** | **Integer** (int32) | The origin room ID of the file entry. | [optional] |
| **originTitle** | **String** | The origin title of the file entry. | [optional] [nullable] |
| **originRoomTitle** | **String** | The origin room title of the file entry. | [optional] [nullable] |
| **canShare** | **Boolean** | Specifies if the file entry can be shared or not. | [optional] |
| **shareSettings** | [**FileEntryDtoInteger_allOf_shareSettings**](#model-fileentrydtointegersharesettings) |  | [optional] [nullable] |
| **security** | [**FileEntryDtoInteger_allOf_security**](#model-fileentrydtointegersecurity) |  | [optional] [nullable] |
| **availableShareRights** | [**FileEntryDtoInteger_allOf_availableShareRights**](#model-fileentrydtointegeravailablesharerights) |  | [optional] [nullable] |
| **requestToken** | **String** | The request token of the file entry. | [optional] [nullable] |
| **external** | **Boolean** | Specifies if the folder can be accessed via an external link or not. | [optional] [nullable] |
| **expirationDate** | [**ApiDateTime**](#model-apidatetime) |  | [optional] |
| **isLinkExpired** | **Boolean** | Indicates whether the shareable link associated with the file or folder has expired. | [optional] [nullable] |
| **folderId** | **Integer** (int32) | The folder ID where the file is located. | [optional] |
| **version** | **Integer** (int32) | The file version. | [optional] |
| **versionGroup** | **Integer** (int32) | The version group of the file. | [optional] |
| **contentLength** | **String** | The content length of the file. | [optional] [nullable] |
| **pureContentLength** | **Long** (int64) | The pure content length of the file. | [optional] [nullable] |
| **fileStatus** | [**FileStatus**](#model-filestatus) |  | [optional] [enum: 0, 1, 2, 4, 8, 16, 32, 64, 128, 256] |
| **editingBy** | **Map** | The list of users editing the file. | [optional] [nullable] |
| **mute** | **Boolean** | Specifies if the file is muted or not. | [optional] |
| **viewUrl** | **URI** (uri) | The URL link to view the file. | [optional] [nullable] |
| **webUrl** | **URI** (uri) | The Web URL link to the file. | [optional] [nullable] |
| **fileType** | [**FileType**](#model-filetype) |  | [optional] [enum: 0, 1, 2, 3, 4, 5, 6, 7, 10, 11] |
| **fileExst** | **String** | The file extension. | [optional] [nullable] |
| **comment** | **String** | The comment to the file. | [optional] [nullable] |
| **encrypted** | **Boolean** | Specifies if the file is encrypted or not. | [optional] [nullable] |
| **thumbnailUrl** | **URI** (uri) | The thumbnail URL of the file. | [optional] [nullable] |
| **thumbnailStatus** | [**Thumbnail**](#model-thumbnail) |  | [optional] [enum: 0, 1, 2, 3, 4] |
| **locked** | **Boolean** | Specifies if the file is locked or not. | [optional] [nullable] |
| **lockedBy** | **String** | The user ID of the person who locked the file. | [optional] [nullable] |
| **hasDraft** | **Boolean** | Specifies if the file has a draft or not. | [optional] [nullable] |
| **formFillingStatus** | [**FormFillingStatus**](#model-formfillingstatus) |  | [optional] [enum: 0, 1, 2, 3, 4, 5] |
| **isForm** | **Boolean** | Specifies if the file is a form or not. | [optional] [nullable] |
| **customFilterEnabled** | **Boolean** | Specifies if the Custom Filter editing mode is enabled for a file or not. | [optional] [nullable] |
| **customFilterEnabledBy** | **String** | The name of the user who enabled a Custom Filter editing mode for a file. | [optional] [nullable] |
| **startFilling** | **Boolean** | Specifies if the filling has started or not. | [optional] [nullable] |
| **isFillingPreparing** | **Boolean** | Specifies if the form filling has started but the file is still being saved by the document editor. Filling and editing are not allowed. | [optional] [nullable] |
| **inProcessFolderId** | **Integer** (int32) | The InProcess folder ID of the file. | [optional] [nullable] |
| **inProcessFolderTitle** | **String** | The InProcess folder title of the file. | [optional] [nullable] |
| **resultsFolderId** | **Integer** (int32) | The ID of the FormFillingFolderDone folder that corresponds to this original form. | [optional] [nullable] |
| **draftLocation** | [**DraftLocationInteger**](#model-draftlocationinteger) |  | [optional] |
| **viewAccessibility** | [**FileDtoInteger_allOf_viewAccessibility**](#model-filedtointegerviewaccessibility) |  | [optional] [nullable] |
| **lastOpened** | [**ApiDateTime**](#model-apidatetime) |  | [optional] |
| **expired** | [**ApiDateTime**](#model-apidatetime) |  | [optional] |
| **vectorizationStatus** | [**VectorizationStatus**](#model-vectorizationstatus) |  | [optional] [enum: 0, 1, 2] |
| **externalDbTableName** | **String** | The name of the table in the external database that corresponds to this form. | [optional] [nullable] |
| **dimensions** | [**Size**](#model-size) |  | [optional] |


### Model FileDtoInteger.viewAccessibility
The file accessibility.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **ImageView** | **Boolean** |  | [optional] |
| **MediaView** | **Boolean** |  | [optional] |
| **WebView** | **Boolean** |  | [optional] |
| **WebEdit** | **Boolean** |  | [optional] |
| **WebReview** | **Boolean** |  | [optional] |
| **WebCustomFilterEditing** | **Boolean** |  | [optional] |
| **WebRestrictedEditing** | **Boolean** |  | [optional] |
| **WebComment** | **Boolean** |  | [optional] |
| **CanConvert** | **Boolean** |  | [optional] |
| **MustConvert** | **Boolean** |  | [optional] |


### Model FileEncryptionInfoDto

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **userKeys** | [**List**](#model-encryptionkeydto) |  | [optional] [nullable] |
| **fileKeys** | [**List**](#model-filekeys) |  | [optional] [nullable] |


### Model FileEncryptionInfoWrapper

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **response** | [**FileEncryptionInfoDto**](#model-fileencryptioninfodto) |  | [optional] |
| **count** | **Integer** (int32) | The total number of items in the response | [optional] |
| **links** | [**List**](#model-booleanwrapperlinks-item) | List of links related to the response | [optional] |
| **status** | **Integer** (int32) | HTTP status code of the response | [optional] |
| **statusCode** | **Integer** (int32) | HTTP status code of the response (duplicate of status) | [optional] |


### Model FileEntryBaseArrayWrapper

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **response** | [**List**](#model-fileentrybasedto) |  | [optional] |
| **count** | **Integer** (int32) | The total number of items in the response | [optional] |
| **links** | [**List**](#model-booleanwrapperlinks-item) | List of links related to the response | [optional] |
| **status** | **Integer** (int32) | HTTP status code of the response | [optional] |
| **statusCode** | **Integer** (int32) | HTTP status code of the response (duplicate of status) | [optional] |


### Model FileEntryBaseDto
The file entry information.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **title** | **String** | The file entry title. | [optional] [example: Some title.txt] [nullable] |
| **access** | [**FileShare**](#model-fileshare) |  | [optional] [enum: 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11] |
| **sharedBy** | [**EmployeeDto**](#model-employeedto) |  | [optional] |
| **ownedBy** | [**EmployeeDto**](#model-employeedto) |  | [optional] |
| **shared** | **Boolean** | Specifies if the file entry is shared via link or not. | [optional] [example: false] |
| **sharedForUser** | **Boolean** | Specifies if the file entry is shared for user or not. | [optional] [example: false] |
| **sharedExternal** | **Boolean** | Specifies if the file entry is shared via a public (non-internal) external link. | [optional] [example: false] |
| **parentShared** | **Boolean** | Indicates whether the parent entity is shared. | [optional] [example: false] |
| **shortWebUrl** | **URI** (uri) | The short Web URL. | [optional] [example: http://localhost/s/abc123] [nullable] |
| **created** | [**ApiDateTime**](#model-apidatetime) |  | [optional] |
| **createdBy** | [**EmployeeDto**](#model-employeedto) |  | [optional] |
| **updated** | [**ApiDateTime**](#model-apidatetime) |  | [optional] |
| **autoDelete** | [**ApiDateTime**](#model-apidatetime) |  | [optional] |
| **rootFolderType** | [**FolderType**](#model-foldertype) |  | [optional] [enum: 0, 1, 2, 3, 5, 6, 8, 10, 11, 12, 13, 14, 15, 16, 19, 20, 21, 22, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36] |
| **parentRoomType** | [**FolderType**](#model-foldertype) |  | [optional] [enum: 0, 1, 2, 3, 5, 6, 8, 10, 11, 12, 13, 14, 15, 16, 19, 20, 21, 22, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36] |
| **updatedBy** | [**EmployeeDto**](#model-employeedto) |  | [optional] |
| **providerItem** | **Boolean** | Specifies if the file entry provider is specified or not. | [optional] [example: false] [nullable] |
| **providerKey** | **String** | The provider key of the file entry. | [optional] [example: google-drive] [nullable] |
| **providerId** | **Integer** (int32) | The provider ID of the file entry. | [optional] [example: 1] [nullable] |
| **order** | **String** | The order of the file entry. | [optional] [example: 1] [nullable] |
| **isFavorite** | **Boolean** | Specifies if the file is a favorite or not. | [optional] [example: false] [nullable] |
| **fileEntryType** | [**FileEntryType**](#model-fileentrytype) |  | [optional] [enum: 1, 2] |


### Model FileEntryBaseWrapper

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **response** | [**FileEntryBaseDto**](#model-fileentrybasedto) |  | [optional] |
| **count** | **Integer** (int32) | The total number of items in the response | [optional] |
| **links** | [**List**](#model-booleanwrapperlinks-item) | List of links related to the response | [optional] |
| **status** | **Integer** (int32) | HTTP status code of the response | [optional] |
| **statusCode** | **Integer** (int32) | HTTP status code of the response (duplicate of status) | [optional] |


### Model FileEntryDtoInteger
The generic file entry information.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **title** | **String** | The file entry title. | [optional] [nullable] |
| **access** | [**FileShare**](#model-fileshare) |  | [optional] [enum: 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11] |
| **sharedBy** | [**EmployeeDto**](#model-employeedto) |  | [optional] |
| **ownedBy** | [**EmployeeDto**](#model-employeedto) |  | [optional] |
| **shared** | **Boolean** | Specifies if the file entry is shared via link or not. | [optional] |
| **sharedForUser** | **Boolean** | Specifies if the file entry is shared for user or not. | [optional] |
| **sharedExternal** | **Boolean** | Specifies if the file entry is shared via a public (non-internal) external link. | [optional] |
| **parentShared** | **Boolean** | Indicates whether the parent entity is shared. | [optional] |
| **shortWebUrl** | **URI** (uri) | The short Web URL. | [optional] [nullable] |
| **created** | [**ApiDateTime**](#model-apidatetime) |  | [optional] |
| **createdBy** | [**EmployeeDto**](#model-employeedto) |  | [optional] |
| **updated** | [**ApiDateTime**](#model-apidatetime) |  | [optional] |
| **autoDelete** | [**ApiDateTime**](#model-apidatetime) |  | [optional] |
| **rootFolderType** | [**FolderType**](#model-foldertype) |  | [optional] [enum: 0, 1, 2, 3, 5, 6, 8, 10, 11, 12, 13, 14, 15, 16, 19, 20, 21, 22, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36] |
| **parentRoomType** | [**FolderType**](#model-foldertype) |  | [optional] [enum: 0, 1, 2, 3, 5, 6, 8, 10, 11, 12, 13, 14, 15, 16, 19, 20, 21, 22, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36] |
| **updatedBy** | [**EmployeeDto**](#model-employeedto) |  | [optional] |
| **providerItem** | **Boolean** | Specifies if the file entry provider is specified or not. | [optional] [nullable] |
| **providerKey** | **String** | The provider key of the file entry. | [optional] [nullable] |
| **providerId** | **Integer** (int32) | The provider ID of the file entry. | [optional] [nullable] |
| **order** | **String** | The order of the file entry. | [optional] [nullable] |
| **isFavorite** | **Boolean** | Specifies if the file is a favorite or not. | [optional] [nullable] |
| **fileEntryType** | [**FileEntryType**](#model-fileentrytype) |  | [optional] [enum: 1, 2] |
| **id** | **Integer** (int32) | The file entry ID. | [optional] |
| **rootFolderId** | **Integer** (int32) | The root folder ID of the file entry. | [optional] |
| **originId** | **Integer** (int32) | The origin ID of the file entry. | [optional] |
| **originRoomId** | **Integer** (int32) | The origin room ID of the file entry. | [optional] |
| **originTitle** | **String** | The origin title of the file entry. | [optional] [nullable] |
| **originRoomTitle** | **String** | The origin room title of the file entry. | [optional] [nullable] |
| **canShare** | **Boolean** | Specifies if the file entry can be shared or not. | [optional] |
| **shareSettings** | [**FileEntryDtoInteger_allOf_shareSettings**](#model-fileentrydtointegersharesettings) |  | [optional] [nullable] |
| **security** | [**FileEntryDtoInteger_allOf_security**](#model-fileentrydtointegersecurity) |  | [optional] [nullable] |
| **availableShareRights** | [**FileEntryDtoInteger_allOf_availableShareRights**](#model-fileentrydtointegeravailablesharerights) |  | [optional] [nullable] |
| **requestToken** | **String** | The request token of the file entry. | [optional] [nullable] |
| **external** | **Boolean** | Specifies if the folder can be accessed via an external link or not. | [optional] [nullable] |
| **expirationDate** | [**ApiDateTime**](#model-apidatetime) |  | [optional] |
| **isLinkExpired** | **Boolean** | Indicates whether the shareable link associated with the file or folder has expired. | [optional] [nullable] |


### Model FileEntryDtoInteger.availableShareRights
The available external rights of the file entry.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **User** | **List** |  | [optional] |
| **ExternalLink** | **List** |  | [optional] |
| **Group** | **List** |  | [optional] |
| **InvitationLink** | **List** |  | [optional] |
| **PrimaryExternalLink** | **List** |  | [optional] |


### Model FileEntryDtoInteger.security
The actions that can be performed with the file entry.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **Read** | **Boolean** |  | [optional] |
| **Comment** | **Boolean** |  | [optional] |
| **FillForms** | **Boolean** |  | [optional] |
| **Review** | **Boolean** |  | [optional] |
| **Create** | **Boolean** |  | [optional] |
| **CreateFrom** | **Boolean** |  | [optional] |
| **Edit** | **Boolean** |  | [optional] |
| **Delete** | **Boolean** |  | [optional] |
| **CustomFilter** | **Boolean** |  | [optional] |
| **EditRoom** | **Boolean** |  | [optional] |
| **Rename** | **Boolean** |  | [optional] |
| **ReadHistory** | **Boolean** |  | [optional] |
| **Lock** | **Boolean** |  | [optional] |
| **EditHistory** | **Boolean** |  | [optional] |
| **CopyTo** | **Boolean** |  | [optional] |
| **Copy** | **Boolean** |  | [optional] |
| **MoveTo** | **Boolean** |  | [optional] |
| **Move** | **Boolean** |  | [optional] |
| **Pin** | **Boolean** |  | [optional] |
| **Mute** | **Boolean** |  | [optional] |
| **EditAccess** | **Boolean** |  | [optional] |
| **Duplicate** | **Boolean** |  | [optional] |
| **SubmitToFormGallery** | **Boolean** |  | [optional] |
| **Download** | **Boolean** |  | [optional] |
| **Convert** | **Boolean** |  | [optional] |
| **CopySharedLink** | **Boolean** |  | [optional] |
| **ReadLinks** | **Boolean** |  | [optional] |
| **Reconnect** | **Boolean** |  | [optional] |
| **CreateRoomFrom** | **Boolean** |  | [optional] |
| **CopyLink** | **Boolean** |  | [optional] |
| **Embed** | **Boolean** |  | [optional] |
| **ChangeOwner** | **Boolean** |  | [optional] |
| **IndexExport** | **Boolean** |  | [optional] |
| **StartFilling** | **Boolean** |  | [optional] |
| **FillingStatus** | **Boolean** |  | [optional] |
| **ResetFilling** | **Boolean** |  | [optional] |
| **StopFilling** | **Boolean** |  | [optional] |
| **OpenForm** | **Boolean** |  | [optional] |
| **EditInternal** | **Boolean** |  | [optional] |
| **EditExpiration** | **Boolean** |  | [optional] |
| **Vectorization** | **Boolean** |  | [optional] |
| **AskAi** | **Boolean** |  | [optional] |
| **UseChat** | **Boolean** |  | [optional] |
| **UpdateXlsx** | **Boolean** |  | [optional] |
| **AnalyzeResponses** | **Boolean** |  | [optional] |
| **CanUseAi** | **Boolean** |  | [optional] |
| **HistoryExport** | **Boolean** |  | [optional] |


### Model FileEntryDtoInteger.shareSettings
A dictionary representing the sharing settings for the file entry.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **User** | **Integer** (int32) |  | [optional] |
| **ExternalLink** | **Integer** (int32) |  | [optional] |
| **Group** | **Integer** (int32) |  | [optional] |
| **InvitationLink** | **Integer** (int32) |  | [optional] |
| **PrimaryExternalLink** | **Integer** (int32) |  | [optional] |


### Model FileEntryDtoString
The generic file entry information.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **title** | **String** | The file entry title. | [optional] [nullable] |
| **access** | [**FileShare**](#model-fileshare) |  | [optional] [enum: 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11] |
| **sharedBy** | [**EmployeeDto**](#model-employeedto) |  | [optional] |
| **ownedBy** | [**EmployeeDto**](#model-employeedto) |  | [optional] |
| **shared** | **Boolean** | Specifies if the file entry is shared via link or not. | [optional] |
| **sharedForUser** | **Boolean** | Specifies if the file entry is shared for user or not. | [optional] |
| **sharedExternal** | **Boolean** | Specifies if the file entry is shared via a public (non-internal) external link. | [optional] |
| **parentShared** | **Boolean** | Indicates whether the parent entity is shared. | [optional] |
| **shortWebUrl** | **URI** (uri) | The short Web URL. | [optional] [nullable] |
| **created** | [**ApiDateTime**](#model-apidatetime) |  | [optional] |
| **createdBy** | [**EmployeeDto**](#model-employeedto) |  | [optional] |
| **updated** | [**ApiDateTime**](#model-apidatetime) |  | [optional] |
| **autoDelete** | [**ApiDateTime**](#model-apidatetime) |  | [optional] |
| **rootFolderType** | [**FolderType**](#model-foldertype) |  | [optional] [enum: 0, 1, 2, 3, 5, 6, 8, 10, 11, 12, 13, 14, 15, 16, 19, 20, 21, 22, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36] |
| **parentRoomType** | [**FolderType**](#model-foldertype) |  | [optional] [enum: 0, 1, 2, 3, 5, 6, 8, 10, 11, 12, 13, 14, 15, 16, 19, 20, 21, 22, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36] |
| **updatedBy** | [**EmployeeDto**](#model-employeedto) |  | [optional] |
| **providerItem** | **Boolean** | Specifies if the file entry provider is specified or not. | [optional] [nullable] |
| **providerKey** | **String** | The provider key of the file entry. | [optional] [nullable] |
| **providerId** | **Integer** (int32) | The provider ID of the file entry. | [optional] [nullable] |
| **order** | **String** | The order of the file entry. | [optional] [nullable] |
| **isFavorite** | **Boolean** | Specifies if the file is a favorite or not. | [optional] [nullable] |
| **fileEntryType** | [**FileEntryType**](#model-fileentrytype) |  | [optional] [enum: 1, 2] |
| **id** | **String** | The file entry ID. | [optional] [nullable] |
| **rootFolderId** | **String** | The root folder ID of the file entry. | [optional] [nullable] |
| **originId** | **String** | The origin ID of the file entry. | [optional] [nullable] |
| **originRoomId** | **String** | The origin room ID of the file entry. | [optional] [nullable] |
| **originTitle** | **String** | The origin title of the file entry. | [optional] [nullable] |
| **originRoomTitle** | **String** | The origin room title of the file entry. | [optional] [nullable] |
| **canShare** | **Boolean** | Specifies if the file entry can be shared or not. | [optional] |
| **shareSettings** | [**FileEntryDtoInteger_allOf_shareSettings**](#model-fileentrydtointegersharesettings) |  | [optional] [nullable] |
| **security** | [**FileEntryDtoInteger_allOf_security**](#model-fileentrydtointegersecurity) |  | [optional] [nullable] |
| **availableShareRights** | [**FileEntryDtoInteger_allOf_availableShareRights**](#model-fileentrydtointegeravailablesharerights) |  | [optional] [nullable] |
| **requestToken** | **String** | The request token of the file entry. | [optional] [nullable] |
| **external** | **Boolean** | Specifies if the folder can be accessed via an external link or not. | [optional] [nullable] |
| **expirationDate** | [**ApiDateTime**](#model-apidatetime) |  | [optional] |
| **isLinkExpired** | **Boolean** | Indicates whether the shareable link associated with the file or folder has expired. | [optional] [nullable] |


### Model FileEntryIntegerArrayWrapper

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **response** | [**List**](#model-fileentrydtointeger) |  | [optional] |
| **count** | **Integer** (int32) | The total number of items in the response | [optional] |
| **links** | [**List**](#model-booleanwrapperlinks-item) | List of links related to the response | [optional] |
| **status** | **Integer** (int32) | HTTP status code of the response | [optional] |
| **statusCode** | **Integer** (int32) | HTTP status code of the response (duplicate of status) | [optional] |


### Model FileEntryType
[1 - Folder, 2 - File]

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|


### Model FileIntegerArrayWrapper

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **response** | [**List**](#model-filedtointeger) |  | [optional] |
| **count** | **Integer** (int32) | The total number of items in the response | [optional] |
| **links** | [**List**](#model-booleanwrapperlinks-item) | List of links related to the response | [optional] |
| **status** | **Integer** (int32) | HTTP status code of the response | [optional] |
| **statusCode** | **Integer** (int32) | HTTP status code of the response (duplicate of status) | [optional] |


### Model FileIntegerWrapper

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **response** | [**FileDtoInteger**](#model-filedtointeger) |  | [optional] |
| **count** | **Integer** (int32) | The total number of items in the response | [optional] |
| **links** | [**List**](#model-booleanwrapperlinks-item) | List of links related to the response | [optional] |
| **status** | **Integer** (int32) | HTTP status code of the response | [optional] |
| **statusCode** | **Integer** (int32) | HTTP status code of the response (duplicate of status) | [optional] |


### Model FileKeys

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **userId** | **UUID** (uuid) |  | [optional] |
| **publicKeyId** | **UUID** (uuid) |  | [optional] |
| **privateKeyEnc** | **String** |  | [optional] [nullable] |
| **tenantId** | **Integer** (int32) |  | [optional] |
| **fileId** | **Integer** (int32) |  | [optional] |
| **createOn** | **Date** (date-time) |  | [optional] |


### Model FileLink
The file link properties.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **filetype** | **String** | The type of the file for the source viewed or edited document. | [required] [example: docx] [nullable] |
| **token** | **String** | The encrypted signature added to the config in the form of a token. | [optional] [example: token] [nullable] |
| **url** | **URI** (uri) | The absolute URL where the source viewed or edited document is stored. | [required] [example: https://example.com/file.docx] [nullable] |


### Model FileLinkRequest
The external link request parameters.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **linkId** | **UUID** (uuid) | The external link ID. | [optional] [example: 00000000-0000-0000-0000-000000000000] |
| **access** | [**FileShare**](#model-fileshare) |  | [optional] [enum: 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11] |
| **expirationDate** | [**ApiDateTime**](#model-apidatetime) |  | [optional] |
| **title** | **String** | The link name. | [optional] [example: My Document] [minLength: 0] [maxLength: 255] [nullable] |
| **internal** | **Boolean** | The link scope, whether it is internal or not. | [optional] [example: false] |
| **primary** | **Boolean** | Specifies whether the file link is primary or not. | [optional] [example: true] |
| **denyDownload** | **Boolean** | Specifies whether to deny downloading the file or not. | [optional] [example: false] |
| **password** | **String** | Password for access via link. | [optional] [example: p@ssw0rd] [minLength: 0] [maxLength: 255] [nullable] |


### Model FileLinkWrapper

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **response** | [**FileLink**](#model-filelink) |  | [optional] |
| **count** | **Integer** (int32) | The total number of items in the response | [optional] |
| **links** | [**List**](#model-booleanwrapperlinks-item) | List of links related to the response | [optional] |
| **status** | **Integer** (int32) | HTTP status code of the response | [optional] |
| **statusCode** | **Integer** (int32) | HTTP status code of the response (duplicate of status) | [optional] |


### Model FileOperationArrayWrapper

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **response** | [**List**](#model-fileoperationdto) |  | [optional] |
| **count** | **Integer** (int32) | The total number of items in the response | [optional] |
| **links** | [**List**](#model-booleanwrapperlinks-item) | List of links related to the response | [optional] |
| **status** | **Integer** (int32) | HTTP status code of the response | [optional] |
| **statusCode** | **Integer** (int32) | HTTP status code of the response (duplicate of status) | [optional] |


### Model FileOperationDto
The file operation information.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **id** | **String** | The file operation ID. | [required] [example: 00000000-0000-0000-0000-000000000000] [nullable] |
| **Operation** | [**FileOperationType**](#model-fileoperationtype) |  | [required] [enum: 0, 1, 2, 3, 4, 5, 6, 7] |
| **progress** | **Integer** (int32) | The file operation progress in percentage. | [required] [example: 100] |
| **error** | **String** | The file operation error message. | [required] [example: File not found.] [nullable] |
| **processed** | **String** | The file operation processing status. | [required] [example: 1] [nullable] |
| **finished** | **Boolean** | Specifies if the file operation is finished or not. | [required] [example: true] |
| **url** | **URI** (uri) | The file operation URL. | [optional] [example: http://localhost/download] [nullable] |
| **files** | [**List**](#model-fileentrybasedto) | The list of files of the file operation. | [optional] [example: [{"id":10,"title":"document.docx"}]] [nullable] |
| **folders** | [**List**](#model-fileentrybasedto) | The list of folders of the file operation. | [optional] [example: [{"id":20,"title":"My Folder"}]] [nullable] |
| **status** | [**DistributedTaskStatus**](#model-distributedtaskstatus) |  | [optional] [enum: 0, 1, 2, 3, 4] |


### Model FileOperationRequestBaseDto
The base operation request parameters.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **returnSingleOperation** | **Boolean** | Specifies whether to return only the current operation | [optional] [example: false] |


### Model FileOperationType
[0 - Move, 1 - Copy, 2 - Delete, 3 - Download, 4 - MarkAsRead, 5 - Import, 6 - Convert, 7 - Duplicate]

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|


### Model FileOperationWrapper

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **response** | [**FileOperationDto**](#model-fileoperationdto) |  | [optional] |
| **count** | **Integer** (int32) | The total number of items in the response | [optional] |
| **links** | [**List**](#model-booleanwrapperlinks-item) | List of links related to the response | [optional] |
| **status** | **Integer** (int32) | HTTP status code of the response | [optional] |
| **statusCode** | **Integer** (int32) | HTTP status code of the response (duplicate of status) | [optional] |


### Model FileReference
The file reference parameters.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **referenceData** | [**FileReferenceData**](#model-filereferencedata) |  | [optional] |
| **error** | **String** | The error message text. | [optional] [example: Error message] [nullable] |
| **path** | **String** | The file name or relative path for the formula editor. | [optional] [example: /path/file.docx] [nullable] |
| **url** | **URI** (uri) | The URL address to download the current file. | [optional] [example: https://example.com/file.docx] [nullable] |
| **fileType** | **String** | An extension of the document specified with the url parameter. | [optional] [example: docx] [nullable] |
| **key** | **String** | The unique document identifier used by the service to take the data from the co-editing session. | [optional] [example: doc1] [nullable] |
| **link** | **String** | The file URL. | [optional] [example: https://example.com/file.docx] [nullable] |
| **token** | **String** | The encrypted signature added to the parameter in the form of a token. | [optional] [example: token] [nullable] |


### Model FileReferenceData
An object that is generated by the integrator to uniquely identify a file in its system.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **fileKey** | **String** | The unique document identifier used by the service to get a link to the file. | [optional] [example: doc_2026_02_001] [nullable] |
| **instanceId** | **String** | The unique system identifier. | [optional] [example: 00000000-0000-0000-0000-000000000000] [nullable] |
| **roomId** | **String** | Room ID | [optional] [example: 1] [nullable] |
| **canEditRoom** | **Boolean** | Specifies if the room can be edited out or not. | [optional] [example: true] |


### Model FileReferenceWrapper

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **response** | [**FileReference**](#model-filereference) |  | [optional] |
| **count** | **Integer** (int32) | The total number of items in the response | [optional] |
| **links** | [**List**](#model-booleanwrapperlinks-item) | List of links related to the response | [optional] |
| **status** | **Integer** (int32) | HTTP status code of the response | [optional] |
| **statusCode** | **Integer** (int32) | HTTP status code of the response (duplicate of status) | [optional] |


### Model FileShare
[0 - None, 1 - Read and write, 2 - Read, 3 - Restrict, 4 - Varies, 5 - Review, 6 - Comment, 7 - Fill forms, 8 - Custom filter, 9 - Room manager, 10 - Editing, 11 - Content creator]

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|


### Model FileShareArrayWrapper

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **response** | [**List**](#model-filesharedto) |  | [optional] |
| **count** | **Integer** (int32) | The total number of items in the response | [optional] |
| **links** | [**List**](#model-booleanwrapperlinks-item) | List of links related to the response | [optional] |
| **status** | **Integer** (int32) | HTTP status code of the response | [optional] |
| **statusCode** | **Integer** (int32) | HTTP status code of the response (duplicate of status) | [optional] |


### Model FileShareDto
The file sharing information and access rights.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **access** | [**FileShare**](#model-fileshare) |  | [optional] [enum: 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11] |
| **sharedTo** | **oas_any_type_not_mapped** | The user who has the access to the specified file. | [optional] [example: {"displayName":"John Doe"}] [nullable] |
| **sharedToUser** | [**EmployeeFullDto**](#model-employeefulldto) |  | [optional] |
| **sharedToGroup** | [**GroupSummaryDto**](#model-groupsummarydto) |  | [optional] |
| **sharedLink** | [**FileShareLink**](#model-filesharelink) |  | [optional] |
| **isLocked** | **Boolean** | Specifies if the access right is locked or not. | [required] [example: false] |
| **isOwner** | **Boolean** | Specifies if the user is an owner of the specified file or not. | [required] [example: false] |
| **canEditAccess** | **Boolean** | Specifies if the user can edit the access to the specified file or not. | [required] [example: true] |
| **canEditInternal** | **Boolean** | Indicates whether internal editing permissions are granted. | [required] [example: true] |
| **canEditDenyDownload** | **Boolean** | Determines whether the user has permission to modify the deny download setting for the file share. | [required] [example: true] |
| **canEditExpirationDate** | **Boolean** | Indicates whether the expiration date of access permissions can be edited. | [required] [example: true] |
| **canRevoke** | **Boolean** | Specifies whether the file sharing access can be revoked by the current user. | [required] [example: true] |
| **subjectType** | [**SubjectType**](#model-subjecttype) |  | [required] [enum: 0, 1, 2, 3, 4] |


### Model FileShareLink
A shareable link for a file with its configuration and status.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **id** | **UUID** (uuid) | The unique identifier of the shared link. | [optional] [example: 00000000-0000-0000-0000-000000000000] |
| **title** | **String** | The title of the shared content. | [optional] [example: Shared Document] [nullable] |
| **shareLink** | **String** | The URL for accessing the shared content. | [optional] [example: http://localhost/share/abc123] [nullable] |
| **expirationDate** | [**ApiDateTime**](#model-apidatetime) |  | [optional] |
| **linkType** | [**LinkType**](#model-linktype) |  | [optional] [enum: 0, 1] |
| **password** | **String** | The password protection for accessing the shared content. | [optional] [example: password123] [nullable] |
| **denyDownload** | **Boolean** | Indicates whether downloading of the shared content is prohibited. | [optional] [example: false] [nullable] |
| **isExpired** | **Boolean** | Indicates whether the shared link has expired. | [optional] [example: false] [nullable] |
| **primary** | **Boolean** | Indicates whether this is the primary shared link. | [optional] [example: true] |
| **internal** | **Boolean** | Indicates whether the link is for the internal sharing only. | [optional] [example: false] [nullable] |
| **requestToken** | **String** | The token for validating access requests. | [optional] [example: token-abc-123] [nullable] |
| **maxUseCount** | **Integer** (int32) | The maximum number of times the invitation link can be used. | [optional] [example: 10] [nullable] |
| **currentUseCount** | **Integer** (int32) | The current number of times the invitation link has been used. | [optional] [example: 5] [nullable] |


### Model FileShareParams
The collection of file sharing parameters.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **email** | **String** (email) | The email address. | [optional] [maxLength: 255] [nullable] |
| **shareTo** | **UUID** (uuid) | The ID of the user to whom the file will be shared. | [optional] |
| **access** | [**FileShare**](#model-fileshare) |  | [optional] [enum: 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11] |


### Model FileShareWrapper

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **response** | [**FileShareDto**](#model-filesharedto) |  | [optional] |
| **count** | **Integer** (int32) | The total number of items in the response | [optional] |
| **links** | [**List**](#model-booleanwrapperlinks-item) | List of links related to the response | [optional] |
| **status** | **Integer** (int32) | HTTP status code of the response | [optional] |
| **statusCode** | **Integer** (int32) | HTTP status code of the response (duplicate of status) | [optional] |


### Model FileStatus
[0 - None, 1 - Is editing, 2 - Is new, 4 - Is converting, 8 - Is original, 16 - Is editing alone, 32 - Is favorite, 64 - Is template, 128 - Is fill form draft, 256 - Is completed form]

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|


### Model FileType
[0 - Unknown, 1 - Archive, 2 - Video, 3 - Audio, 4 - Image, 5 - Spreadsheet, 6 - Presentation, 7 - Document, 10 - Pdf, 11 - Diagram]

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|


### Model FilesSettingsDto
The file settings parameters.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **extsImagePreviewed** | **List** | The list of extensions of the viewed images. | [optional] [example: [".bmp",".gif",".jpeg",".jpg",".png",".svg"]] [nullable] |
| **extsMediaPreviewed** | **List** | The list of extensions of the viewed media files. | [optional] [example: [".mp4",".webm",".mp3",".ogg"]] [nullable] |
| **extsWebPreviewed** | **List** | The list of extensions of the viewed files. | [optional] [example: [".docx",".xlsx",".pptx",".pdf"]] [nullable] |
| **extsWebEdited** | **List** | The list of extensions of the edited files. | [optional] [example: [".docx",".xlsx",".pptx"]] [nullable] |
| **extsWebEncrypt** | **List** | The list of extensions of the encrypted files. | [optional] [example: [".docx",".xlsx",".pptx"]] [nullable] |
| **extsWebReviewed** | **List** | The list of extensions of the reviewed files. | [optional] [example: [".docx"]] [nullable] |
| **extsWebCustomFilterEditing** | **List** | The list of extensions of the custom filter files. | [optional] [example: [".xlsx"]] [nullable] |
| **extsWebRestrictedEditing** | **List** | The list of extensions of the files that are restricted for editing. | [optional] [example: [".pdf"]] [nullable] |
| **extsWebCommented** | **List** | The list of extensions of the commented files. | [optional] [example: [".docx"]] [nullable] |
| **extsWebTemplate** | **List** | The list of extensions of the template files. | [optional] [example: [".docx",".xlsx",".pptx"]] [nullable] |
| **extsMustConvert** | **List** | The list of extensions of the files that must be converted. | [optional] [example: [".doc",".xls",".ppt"]] [nullable] |
| **extsConvertible** | **Map** | The list of the convertible extensions. | [optional] [example: {".doc":[".docx",".pdf"],".xls":[".xlsx",".pdf"]}] [nullable] |
| **extsUploadable** | **List** | The list of the uploadable extensions. | [optional] [example: [".docx",".xlsx",".pdf"]] [nullable] |
| **extsArchive** | **List** | The list of extensions of the archive files. | [optional] [example: [".zip",".rar",".7z"]] [nullable] |
| **extsVideo** | **List** | The list of the video extensions. | [optional] [example: [".mp4",".webm",".avi"]] [nullable] |
| **extsAudio** | **List** | The list of the audio extensions. | [optional] [example: [".mp3",".ogg",".wav"]] [nullable] |
| **extsImage** | **List** | The list of the image extensions. | [optional] [example: [".png",".jpg",".gif"]] [nullable] |
| **extsSpreadsheet** | **List** | The list of the spreadsheet extensions. | [optional] [example: [".xlsx",".xls",".ods"]] [nullable] |
| **extsPresentation** | **List** | The list of the presentation extensions. | [optional] [example: [".pptx",".ppt",".odp"]] [nullable] |
| **extsDocument** | **List** | The list of the text document extensions. | [optional] [example: [".docx",".doc",".odt"]] [nullable] |
| **extsDiagram** | **List** | The list of the diagram extensions. | [optional] [example: [".vsdx"]] [nullable] |
| **internalFormats** | [**FilesSettingsDto_internalFormats**](#model-filessettingsdtointernalformats) |  | [optional] [nullable] |
| **masterFormExtension** | **String** | The master form extension. | [optional] [example: .docxf] [nullable] |
| **paramVersion** | **String** | The URL parameter which specifies the file version. | [optional] [example: ver] [nullable] |
| **paramOutType** | **String** | The URL parameter which specifies the output type of the converted file. | [optional] [example: otype] [nullable] |
| **fileDownloadUrlString** | **URI** (uri) | The URL to download a file. | [optional] [example: https://example.com/products/files/httphandlers/filehandler.ashx?action=download&fileid={0}] [nullable] |
| **fileWebViewerUrlString** | **String** | The URL to the file web viewer. | [optional] [example: /products/files/doceditor?fileid={0}&action=view] [nullable] |
| **fileWebViewerExternalUrlString** | **URI** (uri) | The external URL to the file web viewer. | [optional] [example: https://example.com/products/files/doceditor?fileid={0}&action=view] [nullable] |
| **fileWebEditorUrlString** | **String** | The URL to the file web editor. | [optional] [example: /products/files/doceditor?fileid={0}&action=edit] [nullable] |
| **fileWebEditorExternalUrlString** | **URI** (uri) | The external URL to the file web editor. | [optional] [example: https://example.com/products/files/doceditor?fileid={0}&action=edit] [nullable] |
| **fileRedirectPreviewUrlString** | **URI** (uri) | The redirect URL to the file viewer. | [optional] [example: https://example.com/products/files/{0}] [nullable] |
| **fileThumbnailUrlString** | **URI** (uri) | The URL to the file thumbnail. | [optional] [example: https://example.com/products/files/httphandlers/filehandler.ashx?action=thumb&fileid={0}] [nullable] |
| **confirmDelete** | **Boolean** | Specifies whether to confirm the file deletion or not. | [optional] [example: true] |
| **enableThirdParty** | **Boolean** | Specifies whether to allow users to connect the third-party storages. | [optional] [example: true] |
| **externalShare** | **Boolean** | Specifies whether to enable sharing external links to the files. | [optional] [example: true] |
| **externalShareSocialMedia** | **Boolean** | Specifies whether to enable sharing files on social media. | [optional] [example: true] |
| **storeOriginalFiles** | **Boolean** | Specifies whether to enable storing original files. | [optional] [example: true] |
| **keepNewFileName** | **Boolean** | Specifies whether to keep the new file name. | [optional] [example: false] |
| **displayFileExtension** | **Boolean** | Specifies whether to display the file extension. | [optional] [example: true] |
| **convertNotify** | **Boolean** | Specifies whether to display the conversion notification. | [optional] [example: true] |
| **hideConfirmCancelOperation** | **Boolean** | Specifies whether to hide the confirmation dialog for the cancel operation. | [optional] [example: false] |
| **hideConfirmConvertSave** | **Boolean** | Specifies whether to hide the confirmation dialog  for saving the file copy in the original format when converting a file. | [optional] [example: false] |
| **hideConfirmConvertOpen** | **Boolean** | Specifies whether to hide the confirmation dialog  for opening the conversion result. | [optional] [example: false] |
| **hideConfirmRoomLifetime** | **Boolean** | Specifies whether to hide the confirmation dialog about the file lifetime in the room. | [optional] [example: false] |
| **defaultOrder** | [**OrderBy**](#model-orderby) |  | [optional] |
| **forcesave** | **Boolean** | Specifies whether to forcesave the files or not. | [optional] [example: false] |
| **storeForcesave** | **Boolean** | Specifies whether to store the forcesaved file versions or not. | [optional] [example: false] |
| **recentSection** | **Boolean** | Specifies if the Recent section is displayed or not. | [optional] [example: true] |
| **favoritesSection** | **Boolean** | Specifies if the Favorites section is displayed or not. | [optional] [example: true] |
| **templatesSection** | **Boolean** | Specifies if the Templates section is displayed or not. | [optional] [example: true] |
| **downloadTarGz** | **Boolean** | Specifies whether to download the .tar.gz files or not. | [optional] [example: true] |
| **automaticallyCleanUp** | [**AutoCleanUpData**](#model-autocleanupdata) |  | [optional] |
| **canSearchByContent** | **Boolean** | Specifies whether the file can be searched by its content or not. | [optional] [example: true] |
| **defaultSharingAccessRights** | **List** | The default access rights in sharing settings. | [optional] [example: [1,2]] [enum: 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11] [nullable] |
| **maxUploadThreadCount** | **Integer** (int32) | The maximum number of upload threads. | [optional] [example: 10] |
| **chunkUploadSize** | **Long** (int64) | The size of a large file that is uploaded in chunks. | [optional] [example: 10485760] |
| **openEditorInSameTab** | **Boolean** | Specifies whether to open the editor in the same tab or not. | [optional] [example: false] |
| **organizeRoomsGrouping** | **Boolean** | Specifies whether the grouping of rooms is enabled or not. | [optional] [example: true] |
| **defaultShareLinkInternal** | **Boolean** | Specifies the default sharing link type: true &#x3D; DocSpace users only (internal), false &#x3D; Anyone with the link. | [optional] [example: false] |
| **externalShareApplyToDocuments** | **Boolean** | When external sharing is restricted, specifies whether the restriction applies to the My Documents section. | [optional] [example: true] |
| **externalShareApplyToRooms** | **Boolean** | When external sharing is restricted, specifies whether the restriction applies to the Rooms section. | [optional] [example: true] |
| **blockExistingLinksOnRestrict** | **Boolean** | When external sharing is restricted, specifies whether existing public links are blocked immediately. | [optional] [example: true] |
| **extsFilesVectorized** | **List** | List of extensions available for vectorization | [optional] [example: [".docx",".pdf",".txt"]] [nullable] |
| **maxVectorizationFileSize** | **Long** (int64) | The maximum file size for vectorization | [optional] [example: 5242880] |


### Model FilesSettingsDto.internalFormats
The internal file formats.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **Unknown** | **String** |  | [optional] |
| **Archive** | **String** |  | [optional] |
| **Video** | **String** |  | [optional] |
| **Audio** | **String** |  | [optional] |
| **Image** | **String** |  | [optional] |
| **Spreadsheet** | **String** |  | [optional] |
| **Presentation** | **String** |  | [optional] |
| **Document** | **String** |  | [optional] |
| **Pdf** | **String** |  | [optional] |
| **Diagram** | **String** |  | [optional] |


### Model FilesSettingsWrapper

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **response** | [**FilesSettingsDto**](#model-filessettingsdto) |  | [optional] |
| **count** | **Integer** (int32) | The total number of items in the response | [optional] |
| **links** | [**List**](#model-booleanwrapperlinks-item) | List of links related to the response | [optional] |
| **status** | **Integer** (int32) | HTTP status code of the response | [optional] |
| **statusCode** | **Integer** (int32) | HTTP status code of the response (duplicate of status) | [optional] |


### Model FilesStatisticsFolder
The file statictics folder parameters.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **title** | **String** | The folder title. | [optional] [example: My Documents] [nullable] |
| **usedSpace** | **Long** (int64) | The used space in the folder. | [optional] [example: 1048576] |


### Model FilesStatisticsResultDto
The file statistics result parameters.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **myDocumentsUsedSpace** | [**FilesStatisticsFolder**](#model-filesstatisticsfolder) |  | [optional] |
| **trashUsedSpace** | [**FilesStatisticsFolder**](#model-filesstatisticsfolder) |  | [optional] |
| **archiveUsedSpace** | [**FilesStatisticsFolder**](#model-filesstatisticsfolder) |  | [optional] |
| **roomsUsedSpace** | [**FilesStatisticsFolder**](#model-filesstatisticsfolder) |  | [optional] |
| **aiAgentsUsedSpace** | [**FilesStatisticsFolder**](#model-filesstatisticsfolder) |  | [optional] |
| **formsUsedSpace** | [**FilesStatisticsFolder**](#model-filesstatisticsfolder) |  | [optional] |


### Model FilesStatisticsResultWrapper

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **response** | [**FilesStatisticsResultDto**](#model-filesstatisticsresultdto) |  | [optional] |
| **count** | **Integer** (int32) | The total number of items in the response | [optional] |
| **links** | [**List**](#model-booleanwrapperlinks-item) | List of links related to the response | [optional] |
| **status** | **Integer** (int32) | HTTP status code of the response | [optional] |
| **statusCode** | **Integer** (int32) | HTTP status code of the response (duplicate of status) | [optional] |


### Model FillingFormResultDtoInteger
The parameters of the form filling result.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **formNumber** | **Integer** (int32) | The filling form number. | [required] [example: 1] |
| **completedForm** | [**FileDtoInteger**](#model-filedtointeger) |  | [optional] |
| **originalForm** | [**FileDtoInteger**](#model-filedtointeger) |  | [optional] |
| **manager** | [**EmployeeFullDto**](#model-employeefulldto) |  | [optional] |
| **roomId** | **Integer** (int32) | The room ID where filling the form. | [required] [example: 123] |
| **isRoomMember** | **Boolean** | Specifies if the manager who fills the form is a room member or not. | [optional] [example: true] |


### Model FillingFormResultIntegerWrapper

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **response** | [**FillingFormResultDtoInteger**](#model-fillingformresultdtointeger) |  | [optional] |
| **count** | **Integer** (int32) | The total number of items in the response | [optional] |
| **links** | [**List**](#model-booleanwrapperlinks-item) | List of links related to the response | [optional] |
| **status** | **Integer** (int32) | HTTP status code of the response | [optional] |
| **statusCode** | **Integer** (int32) | HTTP status code of the response (duplicate of status) | [optional] |


### Model FilterType
[0 - None, 1 - Files  only, 2 - Folders only, 3 - Documents only, 4 - Presentations only, 5 - Spreadsheets only, 7 - Images only, 8 - By user, 9 - By department, 10 - Archive only, 11 - By extension, 12 - Media only, 13 - Filling forms rooms, 14 - Editing rooms, 17 - Custom rooms, 20 - Public rooms, 22 - Pdf, 23 - Pdf form, 24 - Virtual data rooms, 25 - Diagrams only, 26 - Ai rooms]

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|


### Model FolderContentDtoInteger
The folder content information.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **files** | [**List**](#model-fileentrybasedto) | The list of files in the folder. | [optional] [example: [{"id":10,"title":"document.docx"}]] [nullable] |
| **folders** | [**List**](#model-fileentrybasedto) | The list of folders in the folder. | [optional] [example: [{"id":20,"title":"My Folder"}]] [nullable] |
| **current** | [**FolderDtoInteger**](#model-folderdtointeger) |  | [optional] |
| **pathParts** | **oas_any_type_not_mapped** | The folder path. | [required] [example: {key = "Key", path = "//path//to//folder"}] [nullable] |
| **startIndex** | **Integer** (int32) | The folder start index. | [optional] [example: 0] |
| **count** | **Integer** (int32) | The number of folder elements. | [optional] [example: 4] |
| **total** | **Integer** (int32) | The total number of elements in the folder. | [required] [example: 4] |
| **new** | **Integer** (int32) | The new element index in the folder. | [optional] [example: 0] |


### Model FolderContentIntegerArrayWrapper

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **response** | [**List**](#model-foldercontentdtointeger) |  | [optional] |
| **count** | **Integer** (int32) | The total number of items in the response | [optional] |
| **links** | [**List**](#model-booleanwrapperlinks-item) | List of links related to the response | [optional] |
| **status** | **Integer** (int32) | HTTP status code of the response | [optional] |
| **statusCode** | **Integer** (int32) | HTTP status code of the response (duplicate of status) | [optional] |


### Model FolderContentIntegerWrapper

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **response** | [**FolderContentDtoInteger**](#model-foldercontentdtointeger) |  | [optional] |
| **count** | **Integer** (int32) | The total number of items in the response | [optional] |
| **links** | [**List**](#model-booleanwrapperlinks-item) | List of links related to the response | [optional] |
| **status** | **Integer** (int32) | HTTP status code of the response | [optional] |
| **statusCode** | **Integer** (int32) | HTTP status code of the response (duplicate of status) | [optional] |


### Model FolderDtoInteger
The folder parameters.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **title** | **String** | The file entry title. | [optional] [nullable] |
| **access** | [**FileShare**](#model-fileshare) |  | [optional] [enum: 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11] |
| **sharedBy** | [**EmployeeDto**](#model-employeedto) |  | [optional] |
| **ownedBy** | [**EmployeeDto**](#model-employeedto) |  | [optional] |
| **shared** | **Boolean** | Specifies if the file entry is shared via link or not. | [optional] |
| **sharedForUser** | **Boolean** | Specifies if the file entry is shared for user or not. | [optional] |
| **sharedExternal** | **Boolean** | Specifies if the file entry is shared via a public (non-internal) external link. | [optional] |
| **parentShared** | **Boolean** | Indicates whether the parent entity is shared. | [optional] |
| **shortWebUrl** | **URI** (uri) | The short Web URL. | [optional] [nullable] |
| **created** | [**ApiDateTime**](#model-apidatetime) |  | [optional] |
| **createdBy** | [**EmployeeDto**](#model-employeedto) |  | [optional] |
| **updated** | [**ApiDateTime**](#model-apidatetime) |  | [optional] |
| **autoDelete** | [**ApiDateTime**](#model-apidatetime) |  | [optional] |
| **rootFolderType** | [**FolderType**](#model-foldertype) |  | [optional] [enum: 0, 1, 2, 3, 5, 6, 8, 10, 11, 12, 13, 14, 15, 16, 19, 20, 21, 22, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36] |
| **parentRoomType** | [**FolderType**](#model-foldertype) |  | [optional] [enum: 0, 1, 2, 3, 5, 6, 8, 10, 11, 12, 13, 14, 15, 16, 19, 20, 21, 22, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36] |
| **updatedBy** | [**EmployeeDto**](#model-employeedto) |  | [optional] |
| **providerItem** | **Boolean** | Specifies if the file entry provider is specified or not. | [optional] [nullable] |
| **providerKey** | **String** | The provider key of the file entry. | [optional] [nullable] |
| **providerId** | **Integer** (int32) | The provider ID of the file entry. | [optional] [nullable] |
| **order** | **String** | The order of the file entry. | [optional] [nullable] |
| **isFavorite** | **Boolean** | Specifies if the file is a favorite or not. | [optional] [nullable] |
| **fileEntryType** | [**FileEntryType**](#model-fileentrytype) |  | [optional] [enum: 1, 2] |
| **id** | **Integer** (int32) | The file entry ID. | [optional] |
| **rootFolderId** | **Integer** (int32) | The root folder ID of the file entry. | [optional] |
| **originId** | **Integer** (int32) | The origin ID of the file entry. | [optional] |
| **originRoomId** | **Integer** (int32) | The origin room ID of the file entry. | [optional] |
| **originTitle** | **String** | The origin title of the file entry. | [optional] [nullable] |
| **originRoomTitle** | **String** | The origin room title of the file entry. | [optional] [nullable] |
| **canShare** | **Boolean** | Specifies if the file entry can be shared or not. | [optional] |
| **shareSettings** | [**FileEntryDtoInteger_allOf_shareSettings**](#model-fileentrydtointegersharesettings) |  | [optional] [nullable] |
| **security** | [**FileEntryDtoInteger_allOf_security**](#model-fileentrydtointegersecurity) |  | [optional] [nullable] |
| **availableShareRights** | [**FileEntryDtoInteger_allOf_availableShareRights**](#model-fileentrydtointegeravailablesharerights) |  | [optional] [nullable] |
| **requestToken** | **String** | The request token of the file entry. | [optional] [nullable] |
| **external** | **Boolean** | Specifies if the folder can be accessed via an external link or not. | [optional] [nullable] |
| **expirationDate** | [**ApiDateTime**](#model-apidatetime) |  | [optional] |
| **isLinkExpired** | **Boolean** | Indicates whether the shareable link associated with the file or folder has expired. | [optional] [nullable] |
| **parentId** | **Integer** (int32) | The parent folder ID of the folder. | [optional] |
| **filesCount** | **Integer** (int32) | The number of files that the folder contains. | [optional] |
| **foldersCount** | **Integer** (int32) | The number of folders that the folder contains. | [optional] |
| **isShareable** | **Boolean** | Specifies if the folder can be shared or not. | [optional] [nullable] |
| **new** | **Integer** (int32) | The new element index in the folder. | [optional] |
| **mute** | **Boolean** | Specifies if the folder notifications are enabled or not. | [optional] |
| **tags** | **List** | The list of tags of the folder. | [optional] [nullable] |
| **logo** | [**Logo**](#model-logo) |  | [optional] |
| **pinned** | **Boolean** | Specifies if the folder is pinned or not. | [optional] |
| **roomType** | [**RoomType**](#model-roomtype) |  | [optional] [enum: 1, 2, 5, 6, 8, 9] |
| **private** | **Boolean** | Specifies if the folder is private or not. | [optional] |
| **indexing** | **Boolean** | Specifies if the folder is indexed or not. | [optional] |
| **denyDownload** | **Boolean** | Specifies if the folder can be downloaded or not. | [optional] |
| **lifetime** | [**RoomDataLifetimeDto**](#model-roomdatalifetimedto) |  | [optional] |
| **watermark** | [**WatermarkDto**](#model-watermarkdto) |  | [optional] |
| **type** | [**FolderType**](#model-foldertype) |  | [optional] [enum: 0, 1, 2, 3, 5, 6, 8, 10, 11, 12, 13, 14, 15, 16, 19, 20, 21, 22, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36] |
| **inRoom** | **Boolean** | Specifies if the folder is placed in the room or not. | [optional] [nullable] |
| **quotaLimit** | **Long** (int64) | The folder quota limit. | [optional] [nullable] |
| **isCustomQuota** | **Boolean** | Specifies if the folder room has a custom quota or not. | [optional] [nullable] |
| **usedSpace** | **Long** (int64) | How much folder space is used (counter). | [optional] [nullable] |
| **passwordProtected** | **Boolean** | Specifies if the folder is password protected or not. | [optional] [nullable] |
| **expired** | **Boolean** | Specifies if an external link to the folder is expired or not. | [optional] [nullable] |
| **chatSettings** | [**ChatSettingsDto**](#model-chatsettingsdto) |  | [optional] |
| **rootRoomType** | [**RoomType**](#model-roomtype) |  | [optional] [enum: 1, 2, 5, 6, 8, 9] |
| **saveFormAsXLSX** | **Boolean** | Specifies whether to save form data as XLSX file. | [optional] [nullable] |
| **sendFormToExternalDB** | **Boolean** | Specifies whether to send form data to external database. | [optional] [nullable] |
| **originalFormId** | **Integer** (int32) | The original form ID that corresponds to this FormFillingFolderDone folder. | [optional] [nullable] |


### Model FolderDtoString
The folder parameters.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **title** | **String** | The file entry title. | [optional] [nullable] |
| **access** | [**FileShare**](#model-fileshare) |  | [optional] [enum: 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11] |
| **sharedBy** | [**EmployeeDto**](#model-employeedto) |  | [optional] |
| **ownedBy** | [**EmployeeDto**](#model-employeedto) |  | [optional] |
| **shared** | **Boolean** | Specifies if the file entry is shared via link or not. | [optional] |
| **sharedForUser** | **Boolean** | Specifies if the file entry is shared for user or not. | [optional] |
| **sharedExternal** | **Boolean** | Specifies if the file entry is shared via a public (non-internal) external link. | [optional] |
| **parentShared** | **Boolean** | Indicates whether the parent entity is shared. | [optional] |
| **shortWebUrl** | **URI** (uri) | The short Web URL. | [optional] [nullable] |
| **created** | [**ApiDateTime**](#model-apidatetime) |  | [optional] |
| **createdBy** | [**EmployeeDto**](#model-employeedto) |  | [optional] |
| **updated** | [**ApiDateTime**](#model-apidatetime) |  | [optional] |
| **autoDelete** | [**ApiDateTime**](#model-apidatetime) |  | [optional] |
| **rootFolderType** | [**FolderType**](#model-foldertype) |  | [optional] [enum: 0, 1, 2, 3, 5, 6, 8, 10, 11, 12, 13, 14, 15, 16, 19, 20, 21, 22, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36] |
| **parentRoomType** | [**FolderType**](#model-foldertype) |  | [optional] [enum: 0, 1, 2, 3, 5, 6, 8, 10, 11, 12, 13, 14, 15, 16, 19, 20, 21, 22, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36] |
| **updatedBy** | [**EmployeeDto**](#model-employeedto) |  | [optional] |
| **providerItem** | **Boolean** | Specifies if the file entry provider is specified or not. | [optional] [nullable] |
| **providerKey** | **String** | The provider key of the file entry. | [optional] [nullable] |
| **providerId** | **Integer** (int32) | The provider ID of the file entry. | [optional] [nullable] |
| **order** | **String** | The order of the file entry. | [optional] [nullable] |
| **isFavorite** | **Boolean** | Specifies if the file is a favorite or not. | [optional] [nullable] |
| **fileEntryType** | [**FileEntryType**](#model-fileentrytype) |  | [optional] [enum: 1, 2] |
| **id** | **String** | The file entry ID. | [optional] [nullable] |
| **rootFolderId** | **String** | The root folder ID of the file entry. | [optional] [nullable] |
| **originId** | **String** | The origin ID of the file entry. | [optional] [nullable] |
| **originRoomId** | **String** | The origin room ID of the file entry. | [optional] [nullable] |
| **originTitle** | **String** | The origin title of the file entry. | [optional] [nullable] |
| **originRoomTitle** | **String** | The origin room title of the file entry. | [optional] [nullable] |
| **canShare** | **Boolean** | Specifies if the file entry can be shared or not. | [optional] |
| **shareSettings** | [**FileEntryDtoInteger_allOf_shareSettings**](#model-fileentrydtointegersharesettings) |  | [optional] [nullable] |
| **security** | [**FileEntryDtoInteger_allOf_security**](#model-fileentrydtointegersecurity) |  | [optional] [nullable] |
| **availableShareRights** | [**FileEntryDtoInteger_allOf_availableShareRights**](#model-fileentrydtointegeravailablesharerights) |  | [optional] [nullable] |
| **requestToken** | **String** | The request token of the file entry. | [optional] [nullable] |
| **external** | **Boolean** | Specifies if the folder can be accessed via an external link or not. | [optional] [nullable] |
| **expirationDate** | [**ApiDateTime**](#model-apidatetime) |  | [optional] |
| **isLinkExpired** | **Boolean** | Indicates whether the shareable link associated with the file or folder has expired. | [optional] [nullable] |
| **parentId** | **String** | The parent folder ID of the folder. | [optional] [nullable] |
| **filesCount** | **Integer** (int32) | The number of files that the folder contains. | [optional] |
| **foldersCount** | **Integer** (int32) | The number of folders that the folder contains. | [optional] |
| **isShareable** | **Boolean** | Specifies if the folder can be shared or not. | [optional] [nullable] |
| **new** | **Integer** (int32) | The new element index in the folder. | [optional] |
| **mute** | **Boolean** | Specifies if the folder notifications are enabled or not. | [optional] |
| **tags** | **List** | The list of tags of the folder. | [optional] [nullable] |
| **logo** | [**Logo**](#model-logo) |  | [optional] |
| **pinned** | **Boolean** | Specifies if the folder is pinned or not. | [optional] |
| **roomType** | [**RoomType**](#model-roomtype) |  | [optional] [enum: 1, 2, 5, 6, 8, 9] |
| **private** | **Boolean** | Specifies if the folder is private or not. | [optional] |
| **indexing** | **Boolean** | Specifies if the folder is indexed or not. | [optional] |
| **denyDownload** | **Boolean** | Specifies if the folder can be downloaded or not. | [optional] |
| **lifetime** | [**RoomDataLifetimeDto**](#model-roomdatalifetimedto) |  | [optional] |
| **watermark** | [**WatermarkDto**](#model-watermarkdto) |  | [optional] |
| **type** | [**FolderType**](#model-foldertype) |  | [optional] [enum: 0, 1, 2, 3, 5, 6, 8, 10, 11, 12, 13, 14, 15, 16, 19, 20, 21, 22, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36] |
| **inRoom** | **Boolean** | Specifies if the folder is placed in the room or not. | [optional] [nullable] |
| **quotaLimit** | **Long** (int64) | The folder quota limit. | [optional] [nullable] |
| **isCustomQuota** | **Boolean** | Specifies if the folder room has a custom quota or not. | [optional] [nullable] |
| **usedSpace** | **Long** (int64) | How much folder space is used (counter). | [optional] [nullable] |
| **passwordProtected** | **Boolean** | Specifies if the folder is password protected or not. | [optional] [nullable] |
| **expired** | **Boolean** | Specifies if an external link to the folder is expired or not. | [optional] [nullable] |
| **chatSettings** | [**ChatSettingsDto**](#model-chatsettingsdto) |  | [optional] |
| **rootRoomType** | [**RoomType**](#model-roomtype) |  | [optional] [enum: 1, 2, 5, 6, 8, 9] |
| **saveFormAsXLSX** | **Boolean** | Specifies whether to save form data as XLSX file. | [optional] [nullable] |
| **sendFormToExternalDB** | **Boolean** | Specifies whether to send form data to external database. | [optional] [nullable] |
| **originalFormId** | **Integer** (int32) | The original form ID that corresponds to this FormFillingFolderDone folder. | [optional] [nullable] |


### Model FolderIntegerArrayWrapper

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **response** | [**List**](#model-folderdtointeger) |  | [optional] |
| **count** | **Integer** (int32) | The total number of items in the response | [optional] |
| **links** | [**List**](#model-booleanwrapperlinks-item) | List of links related to the response | [optional] |
| **status** | **Integer** (int32) | HTTP status code of the response | [optional] |
| **statusCode** | **Integer** (int32) | HTTP status code of the response (duplicate of status) | [optional] |


### Model FolderIntegerWrapper

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **response** | [**FolderDtoInteger**](#model-folderdtointeger) |  | [optional] |
| **count** | **Integer** (int32) | The total number of items in the response | [optional] |
| **links** | [**List**](#model-booleanwrapperlinks-item) | List of links related to the response | [optional] |
| **status** | **Integer** (int32) | HTTP status code of the response | [optional] |
| **statusCode** | **Integer** (int32) | HTTP status code of the response (duplicate of status) | [optional] |


### Model FolderLinkRequest
The folder link parameters.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **linkId** | **UUID** (uuid) | The folder link ID. | [optional] [example: 00000000-0000-0000-0000-000000000000] |
| **access** | [**FileShare**](#model-fileshare) |  | [optional] [enum: 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11] |
| **expirationDate** | [**ApiDateTime**](#model-apidatetime) |  | [optional] |
| **title** | **String** | The link name. | [optional] [example: My Document] [minLength: 0] [maxLength: 255] [nullable] |
| **password** | **String** | The link password. | [optional] [example: p@ssw0rd] [minLength: 0] [maxLength: 255] [nullable] |
| **denyDownload** | **Boolean** | Specifies if downloading the file from the link is disabled or not. | [optional] [example: false] |
| **internal** | **Boolean** | The link scope, whether it is internal or not. | [optional] [example: false] |
| **primary** | **Boolean** | Specifies whether the folder link is primary or not. | [optional] [example: true] |


### Model FolderStringArrayWrapper

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **response** | [**List**](#model-folderdtostring) |  | [optional] |
| **count** | **Integer** (int32) | The total number of items in the response | [optional] |
| **links** | [**List**](#model-booleanwrapperlinks-item) | List of links related to the response | [optional] |
| **status** | **Integer** (int32) | HTTP status code of the response | [optional] |
| **statusCode** | **Integer** (int32) | HTTP status code of the response (duplicate of status) | [optional] |


### Model FolderStringWrapper

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **response** | [**FolderDtoString**](#model-folderdtostring) |  | [optional] |
| **count** | **Integer** (int32) | The total number of items in the response | [optional] |
| **links** | [**List**](#model-booleanwrapperlinks-item) | List of links related to the response | [optional] |
| **status** | **Integer** (int32) | HTTP status code of the response | [optional] |
| **statusCode** | **Integer** (int32) | HTTP status code of the response (duplicate of status) | [optional] |


### Model FolderType
[0 - Default, 1 - Coomon, 2 - Bunch, 3 - Trash, 5 - User, 6 - Share, 8 - Projects, 10 - Favourites, 11 - Recent, 12 - Templates, 13 - Privacy, 14 - Virtual rooms, 15 - Filling forms room, 16 - Editing room, 19 - Custom room, 20 - Archive, 21 - Thirdparty backup, 22 - Public room, 25 - Ready form folder, 26 - In process form folder, 27 - Form filling folder done, 28 - Form filling folder in progress, 29 - Virtual Data Room, 30 - Room templates folder, 31 - AI Room, 32 - Knowledge, 33 - Result storage, 34 - AI Agents, 35 - Default Templates, 36 - Forms]

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|


### Model FormFillingManageAction
[0 - Stop, 1 - Resume, 2 - Start, 3 - Edit]

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|


### Model FormFillingStatus
[0 - None, 1 - Draft, 2 - You turn, 3 - In progress, 4 - Complete, 5 - Stoped]

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|


### Model FormMetadata

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **key** | **String** | The form field key. | [optional] [example: name] [nullable] |
| **type** | **String** | The form field type. | [optional] [example: text] [nullable] |
| **format** | **String** | The form field format. | [optional] [example: date] [nullable] |
| **possibleValues** | **List** | The list of possible values for the form field. | [optional] [example: []] [nullable] |


### Model FormResultsDto

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **createOn** | **Date** (date-time) | The date and time when the form was created. | [optional] |
| **formsData** | [**List**](#model-formsitemdata) | The list of forms data. | [optional] [example: [{"key":"field1","value":"Answer"}]] [nullable] |


### Model FormRole
The form role.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **roomId** | **Integer** (int32) | The room ID. | [optional] [example: 1] |
| **roleName** | **String** | The role name. | [optional] [example: Manager] [nullable] |
| **roleColor** | **String** | The role color. | [optional] [example: #4781D1] [nullable] |
| **userId** | **UUID** (uuid) | The user ID. | [optional] [example: 00000000-0000-0000-0000-000000000000] |
| **sequence** | **Integer** (int32) | The role sequence. | [optional] [example: 12] |
| **submitted** | **Boolean** | Specifies if the role was submitted or not. | [optional] [example: false] |
| **openedAt** | **Date** (date-time) | The date and time when the role was opened. | [optional] [example: 2026-01-01T10:00:00Z] |
| **submissionDate** | **Date** (date-time) | The date and time when the role was submitted. | [optional] [example: 2026-01-01T10:00:00Z] |


### Model FormRoleArrayWrapper

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **response** | [**List**](#model-formroledto) |  | [optional] |
| **count** | **Integer** (int32) | The total number of items in the response | [optional] |
| **links** | [**List**](#model-booleanwrapperlinks-item) | List of links related to the response | [optional] |
| **status** | **Integer** (int32) | HTTP status code of the response | [optional] |
| **statusCode** | **Integer** (int32) | HTTP status code of the response (duplicate of status) | [optional] |


### Model FormRoleDto
The form role parameters.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **roleName** | **String** | The role name. | [required] [example: Approver] [nullable] |
| **roleColor** | **String** | The role color. | [optional] [example: #FF5733] [nullable] |
| **user** | [**EmployeeFullDto**](#model-employeefulldto) |  | [optional] |
| **sequence** | **Integer** (int32) | The role sequence. | [required] [example: 1] |
| **submitted** | **Boolean** | Specifies if the role is submitted. | [required] [example: false] |
| **stopedBy** | [**EmployeeFullDto**](#model-employeefulldto) |  | [optional] |
| **history** | **Map** (date-time) | The role history. | [optional] [example: {"0":"2025-01-15T10:30:00Z"}] [nullable] |
| **roleStatus** | [**FormFillingStatus**](#model-formfillingstatus) |  | [optional] [enum: 0, 1, 2, 3, 4, 5] |


### Model FormSubmissionsDto

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **metadata** | [**List**](#model-formmetadata) | The form field metadata. | [optional] [example: []] [nullable] |
| **submissions** | [**List**](#model-formresultsdto) | All submissions. | [optional] [example: []] [nullable] |


### Model FormSubmissionsWrapper

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **response** | [**FormSubmissionsDto**](#model-formsubmissionsdto) |  | [optional] |
| **count** | **Integer** (int32) | The total number of items in the response | [optional] |
| **links** | [**List**](#model-booleanwrapperlinks-item) | List of links related to the response | [optional] |
| **status** | **Integer** (int32) | HTTP status code of the response | [optional] |
| **statusCode** | **Integer** (int32) | HTTP status code of the response (duplicate of status) | [optional] |


### Model FormsItemArrayWrapper

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **response** | [**List**](#model-formsitemdto) |  | [optional] |
| **count** | **Integer** (int32) | The total number of items in the response | [optional] |
| **links** | [**List**](#model-booleanwrapperlinks-item) | List of links related to the response | [optional] |
| **status** | **Integer** (int32) | HTTP status code of the response | [optional] |
| **statusCode** | **Integer** (int32) | HTTP status code of the response (duplicate of status) | [optional] |


### Model FormsItemData
The data of the separate form item.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **key** | **String** | The form data key. | [optional] [example: first_name] [nullable] |
| **tag** | **String** | The form data tag. | [optional] [example: personal_info] [nullable] |
| **value** | **String** | The form data value. | [optional] [example: John] [nullable] |
| **type** | **String** | The form data type. | [optional] [example: text] [nullable] |


### Model FormsItemDto
The forms item information.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **key** | **String** | The form item key.              &lt;example&gt;field_name&lt;/example&gt; | [optional] [nullable] |
| **type** | **String** | The form item type.              &lt;example&gt;text&lt;/example&gt; | [optional] [nullable] |


### Model GetReferenceDataDtoInteger
The request parameters for getting reference data.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **fileKey** | **String** | The unique document identifier used by the service to get a link to the file. | [required] [example: doc_key_123] [nullable] |
| **instanceId** | **String** | The unique system identifier. | [required] [example: doc_key_123] [nullable] |
| **sourceFileId** | **Integer** (int32) | The source file ID. | [optional] [example: 1] |
| **path** | **String** | The file name or relative path for the formula editor. | [optional] [example: My Document] [nullable] |
| **link** | **String** | The file link. | [optional] [example: https://example.com] [nullable] |


### Model GobackConfig
The settings for the Open file location menu button and upper right corner button.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **url** | **String** | The absolute URL to the website address which will be opened when clicking the Open file location menu button. | [optional] [example: https://portal.example.com/files/location] [nullable] |


### Model GroupMemberSecurityRequestArrayWrapper

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **response** | [**List**](#model-groupmembersecurityrequestdto) |  | [optional] |
| **count** | **Integer** (int32) | The total number of items in the response | [optional] |
| **links** | [**List**](#model-booleanwrapperlinks-item) | List of links related to the response | [optional] |
| **status** | **Integer** (int32) | HTTP status code of the response | [optional] |
| **statusCode** | **Integer** (int32) | HTTP status code of the response (duplicate of status) | [optional] |


### Model GroupMemberSecurityRequestDto
The group member security information.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **user** | [**EmployeeFullDto**](#model-employeefulldto) |  | [required] |
| **groupAccess** | [**FileShare**](#model-fileshare) |  | [required] [enum: 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11] |
| **userAccess** | [**FileShare**](#model-fileshare) |  | [optional] [enum: 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11] |
| **overridden** | **Boolean** | Specifies if the group access rights are overridden or not. | [required] [example: false] |
| **canEditAccess** | **Boolean** | Specifies if the group member can edit the group access rights or not. | [required] [example: true] |
| **owner** | **Boolean** | Specifies if the group member is a group owner or not. | [required] [example: false] |


### Model GroupSummaryDto
The group summary parameters.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **id** | **UUID** (uuid) | The group ID. | [required] [example: 00000000-0000-0000-0000-000000000000] |
| **name** | **String** | The group name. | [required] [example: Group Name] [nullable] |
| **manager** | **String** | The group manager. | [optional] [example: Jake.Zazhitski] [nullable] |
| **isSystem** | **Boolean** | Indicates whether the group is a system group. | [optional] [example: false] [nullable] |


### Model HideConfirmConvertRequestDto
The request parameters for hiding the confirmation dialog when converting.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **save** | **Boolean** | Specifies whether to set the specified settings or not. | [optional] [example: true] |


### Model HistoryAction
The action performed on the file.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **id** | [**MessageAction**](#model-messageaction) |  | [optional] [enum: 1000, 1001, 1002, 1003, 1004, 1005, 1006, 1007, 1008, 1009, 1010, 1011, 1012, 1013, 1014, 1015, 1016, 1017, 1018, 1019, 1020, 1021, 1022, 1023, 1024, 1025, 1026, 1027, 1028, 1029, 4000, 4001, 4002, 4003, 4004, 4005, 4006, 4007, 4008, 4009, 4010, 4011, 4012, 4013, 4014, 4015, 4016, 4017, 4018, 4019, 4020, 4021, 4022, 4023, 4024, 4025, 4026, 4027, 4028, 4029, 4030, 4031, 4032, 4033, 4034, 4035, 4036, 4037, 5000, 5001, 5002, 5003, 5004, 5005, 5006, 5007, 5008, 5009, 5010, 5011, 5012, 5013, 5014, 5015, 5016, 5017, 5018, 5019, 5020, 5021, 5022, 5023, 5024, 5025, 5026, 5027, 5028, 5029, 5030, 5031, 5032, 5033, 5034, 5035, 5036, 5037, 5038, 5039, 5040, 5041, 5042, 5043, 5044, 5045, 5046, 5047, 5048, 5049, 5050, 5053, 5054, 5055, 5056, 5057, 5058, 5059, 5060, 5061, 5062, 5063, 5064, 5065, 5066, 5068, 5069, 5070, 5071, 5072, 5073, 5074, 5075, 5076, 5077, 5078, 5079, 5080, 5081, 5082, 5083, 5084, 5085, 5086, 5087, 5088, 5089, 5090, 5091, 5092, 5093, 5094, 5095, 5096, 5097, 5098, 5099, 5100, 5101, 5102, 5103, 5104, 5105, 5106, 5107, 5108, 5109, 5110, 5111, 5112, 5113, 5114, 5115, 5116, 5117, 5118, 5119, 5120, 5121, 5122, 5123, 5124, 5125, 5126, 5127, 5128, 5129, 5130, 5131, 5132, 5133, 5150, 5151, 5152, 5153, 5154, 5155, 5156, 5157, 5158, 5159, 5160, 5201, 5202, 5203, 5204, 5205, 5206, 5501, 5502, 5503, 6000, 6001, 6002, 6003, 6004, 6005, 6006, 6007, 6008, 6009, 6010, 6011, 6012, 6013, 6014, 6015, 6016, 6017, 6018, 6019, 6020, 6021, 6022, 6023, 6024, 6025, 6026, 6027, 6028, 6029, 6030, 6031, 6032, 6033, 6034, 6035, 6036, 6037, 6038, 6039, 6040, 6041, 6042, 6043, 6044, 6045, 6046, 6047, 6048, 6049, 6050, 6051, 6052, 6053, 6054, 6055, 6056, 6057, 6058, 6059, 6060, 6061, 6062, 6063, 6064, 6065, 6066, 6067, 6068, 6069, 6070, 6071, 6072, 6073, 6074, 6075, 6076, 6077, 6078, 6079, 6080, 6081, 6082, 6083, 6084, 6085, 6086, 6087, 6088, 6089, 6090, 6091, 6092, 6093, 6094, 6095, 6096, 6097, 6098, 6099, 6100, 6101, 6102, 7000, 7001, 7002, 7003, 7004, 9901, 9902, 9903, 9904, 9905, 9906, 9907, 9908, 9909, -1] |
| **key** | **String** | The action performed on the file. | [optional] [example: fileUploaded] [nullable] |


### Model HistoryArrayWrapper

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **response** | [**List**](#model-historydto) |  | [optional] |
| **count** | **Integer** (int32) | The total number of items in the response | [optional] |
| **links** | [**List**](#model-booleanwrapperlinks-item) | List of links related to the response | [optional] |
| **status** | **Integer** (int32) | HTTP status code of the response | [optional] |
| **statusCode** | **Integer** (int32) | HTTP status code of the response (duplicate of status) | [optional] |


### Model HistoryData
The history data.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **initiatorName** | **String** | The name of the action initiator. | [optional] [example: John Doe] [nullable] |


### Model HistoryDto
The file history information.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **id** | **Integer** (int32) | The unique identifier for the file history entry. | [required] [example: 123] |
| **action** | [**HistoryAction**](#model-historyaction) |  | [required] |
| **initiator** | [**EmployeeDto**](#model-employeedto) |  | [required] |
| **date** | [**ApiDateTime**](#model-apidatetime) |  | [required] |
| **data** | [**HistoryData**](#model-historydata) |  | [required] |
| **related** | [**List**](#model-historydto) | The list of related history. | [optional] [example: [{"id":124,"action":0}]] [nullable] |


### Model ICompressWrapper

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **response** | **Object** | The archiving class unification interface. | [optional] |
| **count** | **Integer** (int32) | The total number of items in the response | [optional] |
| **links** | [**List**](#model-booleanwrapperlinks-item) | List of links related to the response | [optional] |
| **status** | **Integer** (int32) | HTTP status code of the response | [optional] |
| **statusCode** | **Integer** (int32) | HTTP status code of the response (duplicate of status) | [optional] |


### Model IconRequest

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **icon** | **String** | Group icon | [optional] [example: https://example.com/image.png] [nullable] |


### Model InfoConfigDto
The information config parameters.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **favorite** | **Boolean** | Specifies if the file is favorite or not. | [optional] [example: false] [nullable] |
| **folder** | **String** | The folder of the file. | [optional] [example: My Documents] [nullable] |
| **owner** | **String** | The file owner. | [optional] [example: John Doe] [nullable] |
| **sharingSettings** | [**List**](#model-aceshortwrapper) | The sharing settings of the file. | [optional] [example: []] [nullable] |
| **type** | [**EditorType**](#model-editortype) |  | [optional] [enum: 0, 1, 2] |
| **uploaded** | **String** | The uploaded file. | [optional] [example: 2025-01-01T00:00:00] [nullable] |


### Model KeyValuePairBooleanString

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **key** | **Boolean** |  | [optional] |
| **value** | **String** |  | [optional] [nullable] |


### Model KeyValuePairBooleanStringWrapper

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **response** | [**KeyValuePairBooleanString**](#model-keyvaluepairbooleanstring) |  | [optional] |
| **count** | **Integer** (int32) | The total number of items in the response | [optional] |
| **links** | [**List**](#model-booleanwrapperlinks-item) | List of links related to the response | [optional] |
| **status** | **Integer** (int32) | HTTP status code of the response | [optional] |
| **statusCode** | **Integer** (int32) | HTTP status code of the response (duplicate of status) | [optional] |


### Model LinkType
[0 - Invitation, 1 - External]

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|


### Model Location
[1 - Room, 2 - Documents, 3 - Link]

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|


### Model LockFileParameters
The parameters for locking a file.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **lockFile** | **Boolean** | Specifies whether to lock a file or not. | [optional] [example: true] |


### Model Logo
The room logo information.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **original** | **String** | The original logo. | [required] [example: https://portal.example.com/logo/original.png] [nullable] |
| **large** | **String** | The large logo. | [required] [example: https://portal.example.com/logo/large.png] [nullable] |
| **medium** | **String** | The medium logo. | [required] [example: https://portal.example.com/logo/medium.png] [nullable] |
| **small** | **String** | The small logo. | [required] [example: https://portal.example.com/logo/small.png] [nullable] |
| **color** | **String** | The logo color. | [optional] [example: #4781D1] [nullable] |
| **cover** | [**LogoCover**](#model-logocover) |  | [optional] |


### Model LogoConfigDto
The logo config parameters.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **image** | **String** | The image of the logo. | [optional] [example: http://localhost/logo.png] [nullable] |
| **imageDark** | **String** | The dark image of the logo. | [optional] [example: http://localhost/logo-dark.png] [nullable] |
| **imageLight** | **String** | The light image of the logo. | [optional] [example: http://localhost/logo-light.png] [nullable] |
| **imageEmbedded** | **String** | The embedded image of the logo. | [optional] [example: http://localhost/logo-embedded.png] [nullable] |
| **url** | **String** | The url link of the logo. | [optional] [example: http://localhost] [nullable] |
| **visible** | **Boolean** | Specifies if the logo is visible. | [optional] [example: true] |


### Model LogoCover
The logo cover information.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **id** | **String** | The logo cover ID. | [required] [example: default_cover] [nullable] |
| **data** | **String** | The logo cover data. | [required] [example: base64-image-data...] [nullable] |


### Model LogoRequest
The logo request parameters.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **tmpFile** | **String** | The path to the temporary image file. | [required] [example: /tmp/logo.png] [minLength: 1] |
| **x** | **Integer** (int32) | The X coordinate of the rectangle starting point. | [optional] [example: 0] [min: 0] [max: 1280] |
| **y** | **Integer** (int32) | The Y coordinate of the rectangle starting point. | [optional] [example: 0] [min: 0] [max: 1280] |
| **width** | **Integer** (int32) | The rectangle width. | [optional] [example: 100] [min: 1] [max: 1280] |
| **height** | **Integer** (int32) | The rectangle height. | [optional] [example: 100] [min: 1] [max: 1280] |


### Model ManageFormFillingDtoInteger
The parameters for managing form filling.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **formId** | **Integer** (int32) | The ID of the form to manage. | [required] [example: 1] |
| **action** | [**FormFillingManageAction**](#model-formfillingmanageaction) |  | [optional] [enum: 0, 1, 2, 3] |


### Model MentionMessageWrapper
The mention message parameters.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **actionLink** | [**ActionLinkConfig**](#model-actionlinkconfig) |  | [optional] |
| **emails** | **List** | A list of emails that will receive the mention message. | [optional] [example: ["user1@example.com","user2@example.com"]] [nullable] |
| **message** | **String** | The mention message. | [optional] [example: Hello] [minLength: 0] [maxLength: 255] [nullable] |


### Model MentionWrapper
The parameters of a user mentioned in a message.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **user** | [**UserInfo**](#model-userinfo) |  | [optional] |
| **email** | **String** (email) | The user email address. | [optional] [example: user@example.com] [nullable] |
| **id** | **String** | The user unique identification. | [optional] [example: user_0001] [nullable] |
| **image** | **String** | The path to the user&#39;s avatar. | [optional] [example: https://portal.example.com/avatar/user_0001.png] [nullable] |
| **hasAccess** | **Boolean** | Specifies whether the user has the access to the file where they are mentioned. | [optional] [example: true] |
| **name** | **String** | The user full name. | [optional] [example: John Doe] [nullable] |


### Model MentionWrapperArrayWrapper

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **response** | [**List**](#model-mentionwrapper) |  | [optional] |
| **count** | **Integer** (int32) | The total number of items in the response | [optional] |
| **links** | [**List**](#model-booleanwrapperlinks-item) | List of links related to the response | [optional] |
| **status** | **Integer** (int32) | HTTP status code of the response | [optional] |
| **statusCode** | **Integer** (int32) | HTTP status code of the response (duplicate of status) | [optional] |


### Model MessageAction
[1000 - Login success, 1001 - Login success via social account, 1002 - Login fail invalid combination, 1003 - Login fail social account not found, 1004 - Login fail disabled profile, 1005 - Login fail, 1006 - Logout, 1007 - Login success via sms, 1008 - Login fail via sms, 1009 - Login fail ip security, 1010 - Login success via api, 1011 - Login success via social app, 1012 - Login success via api sms, 1013 - Login fail via api, 1014 - Login fail via api sms, 1015 - Login success via SSO, 1016 - Session started, 1017 - Session completed, 1018 - Login fail via SSO, 1019 - Login success via api social account, 1020 - Login fail via api social account, 1021 - Login succes via tfa app, 1022 - Login fail via Tfa app, 1023 - Login fail brute force, 1024 - Login success via api tfa, 1025 - Login fail via api tfa, 1026 - Login fail recaptcha, 1027 - Authorization link activated, 1028 - Login success via OAuth 2.0, 1029 - Login success via login and password, 4000 - User created, 4001 - Guest created, 4002 - User created via invite, 4003 - Guest created via invite, 4004 - User activated, 4005 - Guest activated, 4006 - User updated, 4007 - User updated language, 4008 - User added avatar, 4009 - User deleted avatar, 4010 - User updated avatar thumbnails, 4011 - User linked social account, 4012 - User unlinked social account, 4013 - User sent activation instructions, 4014 - User sent email change instructions, 4015 - User sent password change instructions, 4016 - User sent delete instructions, 4017 - User updated password, 4018 - User deleted, 4019 - Users updated type, 4020 - Users updated status, 4021 - Users sent activation instructions, 4022 - Users deleted, 4023 - Sent invite instructions, 4024 - User imported, 4025 - Guest imported, 4026 - Group created, 4027 - Group updated, 4028 - Group deleted, 4029 - User updated mobile number, 4030 - User data reassigns, 4031 - User data removing, 4032 - User connected tfa app, 4033 - User disconnected tfa app, 4034 - User logout active connections, 4035 - User logout active connection, 4036 - User logout active connections for user, 4037 - Send join invite, 5000 - File created, 5001 - File renamed, 5002 - File updated, 5003 - File created version, 5004 - File deleted version, 5005 - File updated revision comment, 5006 - File locked, 5007 - File unlocked, 5008 - File updated access, 5009 - File downloaded, 5010 - File downloaded as, 5011 - File uploaded, 5012 - File imported, 5013 - File copied, 5014 - File copied with overwriting, 5015 - File moved, 5016 - File moved with overwriting, 5017 - File moved to trash, 5018 - File deleted, 5019 - Folder created, 5020 - Folder renamed, 5021 - Folder updated access, 5022 - Folder copied, 5023 - Folder copied with overwriting, 5024 - Folder moved, 5025 - Folder moved with overwriting, 5026 - Folder moved to trash, 5027 - Folder deleted, 5028 - ThirdParty created, 5029 - ThirdParty updated, 5030 - ThirdParty deleted, 5031 - Documents ThirdParty settings updated, 5032 - Documents overwriting settings updated, 5033 - Documents uploading formats settings updated, 5034 - User file updated, 5035 - File converted, 5036 - File send access link, 5037 - Document service location setting, 5038 - Authorization keys setting, 5039 - Full text search setting, 5040 - Start transfer setting, 5041 - Backup started, 5042 - License key uploaded, 5043 - File change owner, 5044 - File restore version, 5045 - Document send to sign, 5046 - Document sign complete, 5047 - User updated email, 5048 - Documents store forcesave, 5049 - Documents forcesave, 5050 - Start storage encryption, 5053 - Start storage decryption, 5054 - File opened for change, 5055 - File marked as favorite, 5056 - File removed from favorite, 5057 - Folder downloaded, 5058 - File removed from list, 5059 - Folder removed from list, 5060 - File external link access updated, 5061 - Trash emptied, 5062 - File revision downloaded, 5063 - File marked as read, 5064 - File readed, 5065 - Folder marked as read, 5066 - Folder updated access for, 5068 - File updated access for, 5069 - Documents external share settings updated, 5070 - Room created, 5071 - Room renamed, 5072 - Room archived, 5073 - Room unarchived, 5074 - Room deleted, 5075 - Room update access for user, 5076 - Tag created, 5077 - Tags deleted, 5078 - Added room tags, 5079 - Deleted room tags, 5080 - Room logo created, 5081 - Room logo deleted, 5082 - Room invitation link updated, 5083 - Documents keep new file name settings updated, 5084 - Room remove user, 5085 - Room create user, 5086 - Room invitation link created, 5087 - Room invitation link deleted, 5088 - Room external link created, 5089 - Room external link updated, 5090 - Room external link deleted, 5091 - File external link created, 5092 - File external link updated, 5093 - File external link deleted, 5094 - Room group added, 5095 - Room update access for group, 5096 - Room group remove, 5097 - Room external link revoked, 5098 - Room external link renamed, 5099 - File uploaded with overwriting, 5100 - Room copied, 5101 - Documents display file extension updated, 5102 - Room color changed, 5103 - Room cover changed, 5104 - Room indexing changed, 5105 - Room deny download changed, 5106 - Room index export saved, 5107 - Folder index changed, 5108 - Folder index reordered, 5109 - Room deny download enabled, 5110 - Room deny download disabled, 5111 - File index changed, 5112 - Room watermark set, 5113 - Room watermark disabled, 5114 - Room index export saved, 5115 - Room indexing disabled, 5116 - Room life time set, 5117 - Room life time disabled, 5118 - Room invite resend, 5119 - File version deleted, 5120 - File custom filter enabled, 5121 - File custom filter disabled, 5122 - Folder external link created, 5123 - Folder external link updated, 5124 - Folder external link deleted, 5125 - Backup completed, 5126 - Backup failed, 5127 - Scheduled backup started, 5128 - Scheduled backup completed, 5129 - Scheduled backup failed, 5130 - Scheduled backup deleted, 5131 - Backup cancelled, 5132 - Restore started, 5133 - Restore cancelled, 5150 - Form started to fill, 5151 - Form partially filled, 5152 - Form completely filled, 5153 - Form stopped, 5154 - AI agent created, 5155 - AI agent renamed, 5156 - AI agent deleted, 5157 - MCP server added to AI agent, 5158 - MCP server deleted from AI agent, 5159 - Room change owner, 5160 - Documents default templates settings updated, 5201 - File saved, user quota exceeded, 5202 - File not saved due to user quota exceeded, 5203 - File saved, room quota exceeded, 5204 - File not saved due to room quota exceeded, 5205 - File saved, tenant quota exceeded, 5206 - File not saved due to tenant quota exceeded, 5501 - Ldap enabled, 5502 - Ldap disabled, 5503 - LDAP synchronization completed, 6000 - Language settings updated, 6001 - Time zone settings updated, 6002 - Dns settings updated, 6003 - Trusted mail domain settings updated, 6004 - Password strength settings updated, 6005 - Two factor authentication settings updated, 6006 - Administrator message settings updated, 6007 - Default start page settings updated, 6008 - Products list updated, 6009 - Administrator added, 6010 - Administrator opened full access, 6011 - Administrator deleted, 6012 - Users opened product access, 6013 - Groups opened product access, 6014 - Product access opened, 6015 - Product access restricted, 6016 - Product added administrator, 6017 - Product deleted administrator, 6018 - Greeting settings updated, 6019 - Team template changed, 6020 - Color theme changed, 6021 - Owner sent change owner instructions, 6022 - Owner updated, 6023 - Owner sent portal deactivation instructions, 6024 - Owner sent portal delete instructions, 6025 - Portal deactivated, 6026 - Portal deleted, 6027 - Login history report downloaded, 6028 - Audit trail report downloaded, 6029 - SSO enabled, 6030 - SSO disabled, 6031 - Portal access settings updated, 6032 - Cookie settings updated, 6033 - Mail service settings updated, 6034 - Custom navigation settings updated, 6035 - Audit settings updated, 6036 - Two factor authentication disabled, 6037 - Two factor authentication enabled by sms, 6038 - Two factor authentication enabled by tfa app, 6039 - Portal renamed, 6040 - Quota per room changed, 6041 - Quota per room disabled, 6042 - Quota per user changed, 6043 - Quota per user disabled, 6044 - Quota per portal changed, 6045 - Quota per portal disabled, 6046 - Form submit, 6047 - Form opened for filling, 6048 - Custom quota per room default, 6049 - Custom quota per room changed, 6050 - Custom quota per room disabled, 6051 - Custom quota per user default, 6052 - Custom quota per user changed, 6053 - Custom quota per user disabled, 6054 - DevTools access settings changed, 6055 - Webhook created, 6056 - Webhook updated, 6057 - Webhook deleted, 6058 - Created api key, 6059 - Update api key, 6060 - Deleted User api key, 6061 - Customer wallet topped up, 6062 - Customer operation performed, 6063 - Customer operations report downloaded, 6064 - Customer wallet top up settings updated, 6065 - Customer subscription updated, 6066 - Promotional banners visibility settings changed, 6067 - Customer wallet services settings updated, 6068 - Quota per AI agent changed, 6069 - Quota per AI agent disabled, 6070 - Custom quota per AI agent default, 6071 - Custom quota per AI agent changed, 6072 - Custom quota per AI agent disabled, 6073 - AI provider created, 6074 - AI provider updated, 6075 - AI provider deleted, 6076 - MCP server created, 6077 - MCP server updated, 6078 - MCP server enabled, 6079 - MCP server disabled, 6080 - MCP server deleted, 6081 - WebSearch settings configured, 6082 - WebSearch settings reset, 6083 - Vectorization settings configured, 6084 - Vectorization settings reset, 6085 - Webplugin uploaded, 6086 - Webplugin updated, 6087 - Webplugin deleted, 6088 - Whitelabel settings logo text updated, 6089 - Whitelabel settings logos updated, 6090 - Whitelabel company settings updated, 6091 - Whitelabel additional settings updated, 6092 - Whitelabel mail settings updated, 6093 - Invitation settings updated, 6094 - IP restrictions settings updated, 6095 - Login settings updated, 6096 - AI default provider set, 6097 - AI access enabled, 6098 - AI access disabled, 6099 - User AI settings updated, 6100 - Subscription balance moved to wallet, 6101 - Docs Cloud config updated, 6102 - Docs Cloud quota report downloaded, 7000 - Contact admin mail sent, 7001 - Room invite link used, 7002 - User created and added to room, 7003 - Guest created and added to room, 7004 - Contact sales mail sent, 9901 - Create client, 9902 - Update client, 9903 - Regenerate secret, 9904 - Delete client, 9905 - Change client activation, 9906 - Change client visibility, 9907 - Revoke user client, 9908 - Generate authorization code token, 9909 - Generate personal access token, -1 - None]

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|


### Model MobilePhoneActivationStatus
[0 - Not activated, 1 - Activated]

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|


### Model Module
The module information.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **id** | **UUID** (uuid) | The module ID. | [optional] [example: 00000000-0000-0000-0000-000000000000] |
| **appName** | **String** | The module product class name. | [optional] [example: files] [nullable] |
| **title** | **String** | The module product class name. | [optional] [example: Documents] [nullable] |
| **link** | **String** | The URL to the module start page. | [optional] [example: https://example.com] [nullable] |
| **iconUrl** | **String** | The module icon URL. | [optional] [example: https://example.com/icon.svg] [nullable] |
| **imageUrl** | **String** | The module large image URL. | [optional] [example: https://example.com/image.png] [nullable] |
| **helpUrl** | **String** | The module help URL. | [optional] [example: https://example.com/help] [nullable] |
| **description** | **String** | The module description. | [optional] [example: File management] [nullable] |
| **isPrimary** | **Boolean** | Specifies if the module is primary or not. | [optional] [example: true] |


### Model ModuleWrapper

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **response** | [**Module**](#model-module) |  | [optional] |
| **count** | **Integer** (int32) | The total number of items in the response | [optional] |
| **links** | [**List**](#model-booleanwrapperlinks-item) | List of links related to the response | [optional] |
| **status** | **Integer** (int32) | HTTP status code of the response | [optional] |
| **statusCode** | **Integer** (int32) | HTTP status code of the response (duplicate of status) | [optional] |


### Model MultiSizeLogoCover

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **id** | **String** | The logo cover ID. | [required] [example: default_cover] [nullable] |
| **data** | **Map** | The logo cover data. | [required] [example: {"small":"base64...","medium":"base64...","large":"base64..."}] [nullable] |


### Model NewItemsDtoFileEntryBaseDto
The new item parameters.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **date** | [**ApiDateTime**](#model-apidatetime) |  | [required] |
| **items** | [**List**](#model-fileentrybasedto) | The list of items. | [required] [nullable] |


### Model NewItemsDtoRoomNewItemsDto
The new item parameters.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **date** | [**ApiDateTime**](#model-apidatetime) |  | [required] |
| **items** | [**List**](#model-roomnewitemsdto) | The list of items. | [required] [nullable] |


### Model NewItemsFileEntryBaseArrayWrapper

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **response** | [**List**](#model-newitemsdtofileentrybasedto) |  | [optional] |
| **count** | **Integer** (int32) | The total number of items in the response | [optional] |
| **links** | [**List**](#model-booleanwrapperlinks-item) | List of links related to the response | [optional] |
| **status** | **Integer** (int32) | HTTP status code of the response | [optional] |
| **statusCode** | **Integer** (int32) | HTTP status code of the response (duplicate of status) | [optional] |


### Model NewItemsRoomNewItemsArrayWrapper

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **response** | [**List**](#model-newitemsdtoroomnewitemsdto) |  | [optional] |
| **count** | **Integer** (int32) | The total number of items in the response | [optional] |
| **links** | [**List**](#model-booleanwrapperlinks-item) | List of links related to the response | [optional] |
| **status** | **Integer** (int32) | HTTP status code of the response | [optional] |
| **statusCode** | **Integer** (int32) | HTTP status code of the response (duplicate of status) | [optional] |


### Model NoContentResult

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **statusCode** | **Integer** (int32) |  | [optional] |


### Model NoContentResultWrapper

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **response** | [**NoContentResult**](#model-nocontentresult) |  | [optional] |
| **count** | **Integer** (int32) | The total number of items in the response | [optional] |
| **links** | [**List**](#model-booleanwrapperlinks-item) | List of links related to the response | [optional] |
| **status** | **Integer** (int32) | HTTP status code of the response | [optional] |
| **statusCode** | **Integer** (int32) | HTTP status code of the response (duplicate of status) | [optional] |


### Model OAuth20Token

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **access\_token** | **String** |  | [optional] [nullable] |
| **refresh\_token** | **String** |  | [optional] [nullable] |
| **expires\_in** | **Long** (int64) |  | [optional] |
| **client\_id** | **String** |  | [optional] [nullable] |
| **client\_secret** | **String** |  | [optional] [nullable] |
| **redirect\_uri** | **URI** (uri) |  | [optional] [nullable] |
| **timestamp** | **Date** (date-time) |  | [optional] |
| **isExpired** | **Boolean** |  | [optional] |


### Model ObjectArrayWrapper

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **response** | **List** |  | [optional] |
| **count** | **Integer** (int32) | The total number of items in the response | [optional] |
| **links** | [**List**](#model-booleanwrapperlinks-item) | List of links related to the response | [optional] |
| **status** | **Integer** (int32) | HTTP status code of the response | [optional] |
| **statusCode** | **Integer** (int32) | HTTP status code of the response (duplicate of status) | [optional] |


### Model Options
The document options.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **watermark\_on\_draw** | [**WatermarkOnDraw**](#model-watermarkondraw) |  | [optional] |


### Model OrderBy
The sorting parameters.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **is\_asc** | **Boolean** | Specifies if the order is ascending. | [optional] [example: true] |
| **property** | [**SortedByType**](#model-sortedbytype) |  | [optional] [enum: 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12] |


### Model OrderRequestDto
The parameters for ordering requests.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **order** | **Integer** (int32) | The order value. | [optional] [example: 1] [min: 1] [max: 2147483647] |


### Model OrdersItemRequestDtoInteger
An item in the ordering request with its entry type and ID.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **entryId** | **Integer** (int32) | The entry unique identifier (file or folder). | [required] [example: 1] |
| **entryType** | [**FileEntryType**](#model-fileentrytype) |  | [required] [enum: 1, 2] |
| **order** | **Integer** (int32) | The order value. | [required] [example: 1] [min: 1] [max: 2147483647] |


### Model OrdersRequestDtoInteger
The collection of items to be ordered.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **items** | [**List**](#model-ordersitemrequestdtointeger) | The list of items with their ordering information. | [required] [example: [{"entryId":1,"order":1}]] [nullable] |


### Model Paragraph
The paragraph parameters.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **align** | **Integer** (int32) | The paragraph align. | [optional] [example: 2] |
| **runs** | [**List**](#model-run) | The list of text runs from the paragraph. | [optional] [example: [{"fill":[124,124,124],"text":"CONFIDENTIAL","fontSize":26}]] [nullable] |


### Model PermissionsConfig
The permissions configuration parameters.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **comment** | **Boolean** | Defines if the document can be commented or not. | [optional] [example: true] |
| **chat** | **Boolean** | Defines if the chat functionality is enabled in the document or not. | [optional] [example: true] |
| **download** | **Boolean** | Defines if the document can be downloaded or only viewed or edited online. | [optional] [example: true] |
| **edit** | **Boolean** | Defines if the document can be edited or only viewed. | [optional] [example: true] |
| **fillForms** | **Boolean** | Defines if the forms can be filled. | [optional] [example: true] |
| **modifyFilter** | **Boolean** | Defines if the filter can be applied globally (true) affecting all the other users,  or locally (false), i.e. for the current user only. | [optional] [example: true] |
| **protect** | **Boolean** | Defines if the Protection tab on the toolbar and the Protect button in the left menu are displayedor hidden. | [optional] [example: true] |
| **print** | **Boolean** | Defines if the document can be printed or not. | [optional] [example: true] |
| **review** | **Boolean** | Defines if the document can be reviewed or not. | [optional] [example: true] |
| **copy** | **Boolean** | Defines if the content can be copied to the clipboard or not. | [optional] [example: true] |


### Model PluginsConfig
The configuration settings to connect the special add-ons.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **pluginsData** | **List** | The array of absolute URLs to the plugin configuration files. | [optional] [example: ["https://portal.example.com/ThirdParty/plugin/easybib/config.json","https://portal.example.com/ThirdParty/plugin/wordpress/config.json"]] [nullable] |


### Model ProviderArrayWrapper

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **response** | [**List**](#model-providerdto) |  | [optional] |
| **count** | **Integer** (int32) | The total number of items in the response | [optional] |
| **links** | [**List**](#model-booleanwrapperlinks-item) | List of links related to the response | [optional] |
| **status** | **Integer** (int32) | HTTP status code of the response | [optional] |
| **statusCode** | **Integer** (int32) | HTTP status code of the response (duplicate of status) | [optional] |


### Model ProviderDto
The provider information.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **name** | **String** | The provider name. | [optional] [example: GoogleDrive] [nullable] |
| **key** | **String** | The provider key. | [optional] [example: google-drive] [nullable] |
| **connected** | **Boolean** | Specifies whether the provider is connected. | [optional] [example: true] |
| **oauth** | **Boolean** | Specifies if the provider is OAuth. | [optional] [example: true] |
| **redirectUrl** | **String** | The provider redirect URL. | [optional] [example: http://localhost/redirect] [nullable] |
| **requiredConnectionUrl** | **Boolean** | The required connection URL flag. | [optional] [example: false] |
| **clientId** | **String** | The provider OAuth client ID. | [optional] [example: client-id-123] [nullable] |


### Model ProviderFilter
[0 - None, 1 - Box, 2 - DropBox, 3 - GoogleDrive, 4 - kDrive, 5 - OneDrive, 6 - SharePoint, 7 - WebDav, 8 - Yandex, 9 - Storage]

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|


### Model QuotaFilter
[0 - All, 1 - Default, 2 - Custom]

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|


### Model QuotaScope
[0 - User, 1 - Room, 2 - Tenant]

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|


### Model RecentConfig
The presence or absence of the documents in the Open Recent... menu option.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **folder** | **String** | The folder where the document is stored. | [optional] [example: folder_123] [nullable] |
| **title** | **String** | The document title that will be displayed in the Open Recent... menu option. | [optional] [example: Report 2026] [nullable] |
| **url** | **URI** (uri) | The absolute URL to the document where it is stored. | [optional] [example: https://portal.example.com/files/recent/report2026.docx] [nullable] |


### Model ReviewConfig
Configuration for review display settings.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **reviewDisplay** | **String** | The review display string representation. | [optional] [example: full] [nullable] |


### Model RoomDataLifetimeDto
The room data lifetime information.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **deletePermanently** | **Boolean** | Specifies whether to permanently delete the room data or not. | [optional] [example: true] |
| **period** | [**RoomDataLifetimePeriod**](#model-roomdatalifetimeperiod) |  | [optional] [enum: 0, 1, 2] |
| **value** | **Integer** (int32) | Specifies the time period value of the room data lifetime. | [optional] [example: 33] [min: 1] [max: 999] [nullable] |
| **enabled** | **Boolean** | Specifies whether the room data lifetime setting is enabled or not. | [optional] [example: true] [nullable] |


### Model RoomDataLifetimePeriod
[0 - Day, 1 - Month, 2 - Year]

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|


### Model RoomFromTemplateStatusDto
The progress parameters of creating a room from the template.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **roomId** | **Integer** (int32) | The room ID. | [required] [example: 456] |
| **progress** | **Double** (double) | The progress of creating a room from the template. | [required] [example: 50.0] |
| **error** | **String** | The error message that is sent when a room is not created successfully from the template. | [required] [example: Room creation failed] [nullable] |
| **isCompleted** | **Boolean** | Specifies whether the process of creating a room from the template is completed. | [required] [example: false] |


### Model RoomFromTemplateStatusWrapper

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **response** | [**RoomFromTemplateStatusDto**](#model-roomfromtemplatestatusdto) |  | [optional] |
| **count** | **Integer** (int32) | The total number of items in the response | [optional] |
| **links** | [**List**](#model-booleanwrapperlinks-item) | List of links related to the response | [optional] |
| **status** | **Integer** (int32) | HTTP status code of the response | [optional] |
| **statusCode** | **Integer** (int32) | HTTP status code of the response (duplicate of status) | [optional] |


### Model RoomGroupArrayWrapper

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **response** | [**List**](#model-roomgroupdto) |  | [optional] |
| **count** | **Integer** (int32) | The total number of items in the response | [optional] |
| **links** | [**List**](#model-booleanwrapperlinks-item) | List of links related to the response | [optional] |
| **status** | **Integer** (int32) | HTTP status code of the response | [optional] |
| **statusCode** | **Integer** (int32) | HTTP status code of the response (duplicate of status) | [optional] |


### Model RoomGroupDto
The room security parameters.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **id** | **Integer** (int32) | The group ID. | [optional] [example: 1] |
| **name** | **String** | Group name | [optional] [example: My Group] [nullable] |
| **icon** | [**MultiSizeLogoCover**](#model-multisizelogocover) |  | [optional] |
| **userId** | **UUID** (uuid) | The user ID. | [optional] [example: 00000000-0000-0000-0000-000000000000] |
| **rooms** | [**List**](#model-fileentrybasedto) | The list of rooms in the group. | [optional] [example: [{"id":1,"title":"Room 1"},{"id":2,"title":"Room 2"}]] [nullable] |
| **totalRooms** | **Integer** (int32) | Total number of rooms in the group. | [optional] [example: 2] |


### Model RoomGroupRequestDto
The request parameters for creating a room group

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **name** | **String** | Group name | [required] [example: My Group] [minLength: 0] [maxLength: 128] |
| **icon** | **String** | Group icon | [required] [example: cover1] [minLength: 0] [maxLength: 50] |
| **rooms** | [**List**](#model-duplicaterequestdtofileids) | The list of room IDs. | [required] [example: [1,2,3]] |


### Model RoomGroupWrapper

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **response** | [**RoomGroupDto**](#model-roomgroupdto) |  | [optional] |
| **count** | **Integer** (int32) | The total number of items in the response | [optional] |
| **links** | [**List**](#model-booleanwrapperlinks-item) | List of links related to the response | [optional] |
| **status** | **Integer** (int32) | HTTP status code of the response | [optional] |
| **statusCode** | **Integer** (int32) | HTTP status code of the response (duplicate of status) | [optional] |


### Model RoomInvitation
The room invitation parameters.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **email** | **String** (email) | The email address. | [optional] [maxLength: 255] [nullable] |
| **id** | **UUID** (uuid) | The ID of the user to share a room with. | [optional] |
| **access** | [**FileShare**](#model-fileshare) |  | [optional] [enum: 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11] |


### Model RoomInvitationRequest
The request parameters for inviting users to the room.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **invitations** | [**List**](#model-roominvitation) | The collection of invitation parameters. | [optional] [example: [{"id":"00000000-0000-0000-0000-000000000000","access":1}]] [nullable] |
| **notify** | **Boolean** | Specifies whether to notify users about the shared room or not. | [optional] [example: true] |
| **message** | **String** | The message to send when notifying about the shared room. | [optional] [example: You have been invited to the room] [nullable] |
| **culture** | **String** | The language of the room invitation. | [optional] [example: en-US] [nullable] |
| **force** | **Boolean** | Specifies whether to forcibly delete a user with form roles from the room. | [optional] [example: false] |


### Model RoomLinkRequest
The room link parameters.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **linkId** | **UUID** (uuid) | The room link ID. | [optional] [example: 00000000-0000-0000-0000-000000000000] |
| **access** | [**FileShare**](#model-fileshare) |  | [optional] [enum: 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11] |
| **expirationDate** | [**ApiDateTime**](#model-apidatetime) |  | [optional] |
| **internal** | **Boolean** | The link scope, whether it is internal or not. | [optional] [example: false] |
| **title** | **String** | The link name. | [optional] [example: My Document] [minLength: 0] [maxLength: 255] [nullable] |
| **linkType** | [**LinkType**](#model-linktype) |  | [optional] [enum: 0, 1] |
| **password** | **String** | The link password. | [optional] [example: doc_key_123] [minLength: 0] [maxLength: 255] [nullable] |
| **denyDownload** | **Boolean** | Specifies if downloading the file from the link is disabled or not. | [optional] [example: false] |
| **maxUseCount** | **Integer** (int32) | The maximum number of times the invitation link can be used. | [optional] [example: 25] [min: 1] [max: 1000] [nullable] |
| **currentUseCount** | **Integer** (int32) | The current number of times the invitation link has been used. | [optional] [example: 0] |


### Model RoomNewItemsDto
The room new items information.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **room** | [**FileEntryBaseDto**](#model-fileentrybasedto) |  | [optional] |
| **items** | [**List**](#model-fileentrybasedto) | The list of file entry items. | [optional] [nullable] |


### Model RoomPrivacyFilter
[0 - None, 1 - Private, 2 - NotPrivate]

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|


### Model RoomSecurityDto
The room security parameters.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **members** | [**List**](#model-filesharedto) | The list of room members. | [optional] [example: [{"access":1,"isOwner":false}]] [nullable] |
| **warning** | **String** | The warning message. | [optional] [example: Warning message] [nullable] |
| **error** | [**RoomSecurityError**](#model-roomsecurityerror) |  | [optional] [enum: 0, 1] |


### Model RoomSecurityError
[0 - None, 1 - Form role blocking deletion]

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|


### Model RoomSecurityWrapper

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **response** | [**RoomSecurityDto**](#model-roomsecuritydto) |  | [optional] |
| **count** | **Integer** (int32) | The total number of items in the response | [optional] |
| **links** | [**List**](#model-booleanwrapperlinks-item) | List of links related to the response | [optional] |
| **status** | **Integer** (int32) | HTTP status code of the response | [optional] |
| **statusCode** | **Integer** (int32) | HTTP status code of the response (duplicate of status) | [optional] |


### Model RoomTemplateDto
The room template parameters.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **roomId** | **Integer** (int32) | The room template ID. | [required] [example: 1] |
| **title** | **String** | The room template title. | [required] [example: My Document] [minLength: 0] [maxLength: 400] |
| **logo** | [**LogoRequest**](#model-logorequest) |  | [optional] |
| **copyLogo** | **Boolean** | Specifies whether to copy room logo or not. | [optional] [example: true] |
| **share** | **List** | The collection of email addresses of users with whom to share a room. | [optional] [example: ["user1@example.com","user2@example.com"]] [nullable] |
| **groups** | **List** (uuid) | The collection of groups with whom to share a room. | [optional] [example: ["00000000-0000-0000-0000-000000000000"]] [nullable] |
| **public** | **Boolean** | Specifies whether the room template is public or not. | [optional] [example: true] |
| **tags** | **List** | The collection of tags. | [optional] [example: ["tag1","tag2"]] [nullable] |
| **color** | **String** | The color of the room template. | [optional] [example: #FF0000] [minLength: 0] [maxLength: 6] [nullable] |
| **cover** | **String** | The cover of the room template. | [optional] [example: cover1] [minLength: 0] [maxLength: 50] [nullable] |
| **quota** | **Long** (int64) | Room quota | [optional] [example: 10485760] [nullable] |


### Model RoomTemplateStatusDto
The room template status.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **templateId** | **Integer** (int32) | The room template ID. | [required] [example: 123] |
| **progress** | **Double** (double) | The progress of the room template creation process. | [required] [example: 75.5] |
| **error** | **String** | The error message that is sent when the room template is not created successfully. | [optional] [example: Template creation failed] [nullable] |
| **isCompleted** | **Boolean** | Specifies whether the process of creating the room template is completed. | [required] [example: false] |


### Model RoomTemplateStatusWrapper

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **response** | [**RoomTemplateStatusDto**](#model-roomtemplatestatusdto) |  | [optional] |
| **count** | **Integer** (int32) | The total number of items in the response | [optional] |
| **links** | [**List**](#model-booleanwrapperlinks-item) | List of links related to the response | [optional] |
| **status** | **Integer** (int32) | HTTP status code of the response | [optional] |
| **statusCode** | **Integer** (int32) | HTTP status code of the response (duplicate of status) | [optional] |


### Model RoomType
[1 - Form filling room, 2 - Collaboration room, 5 - Custom room, 6 - Public room, 8 - Virtual data room, 9 - AI Room]

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|


### Model Run
The text run parameters.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **fill** | **List** (int32) | The fill color of the text run in RGB format. | [optional] [example: [124,124,124]] [nullable] |
| **text** | **String** | The run text. | [optional] [example: CONFIDENTIAL] [nullable] |
| **font-size** | **String** | The font size of the text run in points. | [optional] [example: 26] [nullable] |


### Model STRINGArrayWrapper

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **response** | **List** |  | [optional] |
| **count** | **Integer** (int32) | The total number of items in the response | [optional] |
| **links** | [**List**](#model-booleanwrapperlinks-item) | List of links related to the response | [optional] |
| **status** | **Integer** (int32) | HTTP status code of the response | [optional] |
| **statusCode** | **Integer** (int32) | HTTP status code of the response (duplicate of status) | [optional] |


### Model SaveAsPdfInteger
The parameters for saving a file as PDF.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **folderId** | **Integer** (int32) | The folder ID to save the file as PDF. | [required] [example: 1] |
| **title** | **String** | The file title to save as PDF. | [required] [example: My Document] [nullable] |


### Model SaveFormRoleMappingDtoInteger
The parameters for saving form role mapping.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **formId** | **Integer** (int32) | The form ID. | [required] [example: 1] |
| **roles** | [**List**](#model-formrole) | The collection of roles. | [required] [example: [{"roleName":"Approver","userId":"00000000-0000-0000-0000-000000000000"}]] [nullable] |


### Model SearchArea
[0 - Active, 1 - Archive, 2 - Any, 3 - Recent by links, 4 - Template, 5 - Knowledge, 6 - Result storage, 7 - AiAgents, 8 - Forms, 9 - Form templates]

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|


### Model SecurityInfoRequestDto
The security information request parameters.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **folderIds** | [**List**](#model-duplicaterequestdtofileids) | The list of the shared folder IDs. | [optional] [example: [1,2,3]] [nullable] |
| **fileIds** | [**List**](#model-duplicaterequestdtofileids) | The list of the shared file IDs. | [optional] [example: [1,2,3]] [nullable] |
| **share** | [**List**](#model-fileshareparams) | The collection of sharing parameters. | [optional] [example: [{"access":1,"shareTo":"00000000-0000-0000-0000-000000000000"}]] [nullable] |
| **notify** | **Boolean** | Specifies whether to notify users about the shared file or not. | [optional] [example: true] |
| **sharingMessage** | **String** | The message to send when notifying about the shared file. | [optional] [example: You have been granted access to the file] [minLength: 0] [maxLength: 255] [nullable] |


### Model SecurityInfoSimpleRequestDto
The parameters of the security information request.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **share** | [**List**](#model-fileshareparams) | The collection of sharing parameters. | [optional] [example: [{"access":1,"shareTo":"00000000-0000-0000-0000-000000000000"}]] [nullable] |
| **notify** | **Boolean** | Specifies whether to notify users about the shared file or not. | [optional] [example: true] |
| **sharingMessage** | **String** | The message to send when notifying about the shared file. | [optional] [example: You have been granted access to the file] [minLength: 0] [maxLength: 255] [nullable] |


### Model SessionRequest
The session request parameters.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **fileName** | **String** | The file name. | [required] [example: My Document.docx] [nullable] |
| **fileSize** | **Long** (int64) | The file size. | [optional] [example: 10485760] |
| **relativePath** | **String** | The relative path to the file. | [optional] [example: subfolder/documents] [nullable] |
| **createOn** | [**ApiDateTime**](#model-apidatetime) |  | [optional] |
| **encrypted** | **Boolean** | Specifies whether the file is encrypted or not. | [optional] [example: false] |
| **createNewIfExist** | **Boolean** | Specifies whether to create a new file if it already exists. | [optional] [example: true] |


### Model SetPublicDto
The public settings of the room template to set.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **id** | **Integer** (int32) | The room template ID. | [required] [example: 1] |
| **public** | **Boolean** | Specifies whether the room template is public or not. | [optional] [example: true] |


### Model SettingsRequestDto
The settings request parameters.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **set** | **Boolean** | Specifies whether to set the specified settings or not. | [optional] [example: true] |


### Model ShareFilterType
[0 - User or group, 1 - Invitation link, 2 - External link, 4 - Additional external link, 8 - Primary external link, 16 - User, 32 - Group]

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|


### Model Size
Represents dimensions with width and height values.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **height** | **Integer** (int32) | Gets or sets the height dimension of an object, typically measured in pixels or other unit.  It defines the vertical size of the object. | [optional] [example: 10] |
| **width** | **Integer** (int32) | Gets or sets the width dimension of an object, typically measured in pixels or other unit. | [optional] [example: 10] |


### Model SortOrder
[0 - Ascending, 1 - Descending]

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|


### Model SortedByType
[0 - Date and time, 1 - AZ, 2 - Size, 3 - Author, 4 - Type, 5 - New, 6 - Date and time creation, 7 - Room type, 8 - Tags, 9 - Room, 10 - Custom order, 11 - Last opened, 12 - Used space]

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|


### Model StartEdit
The parameters for starting file editing.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **editingAlone** | **Boolean** | Specifies whether to share the file with other users for editing or not. | [optional] [example: false] |


### Model StartFillingForm
The parameters of the button that starts filling out the form.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **text** | **String** | The caption of the button that starts filling out the form. | [optional] [example: Start Filling] [nullable] |


### Model StartFillingMode
[0 - None, 1 - Share to fill out, 2 - Start filling, 3 - Start filling form room]

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|


### Model Status
[0 - Ok, 1 - Invalid, 2 - Expired, 3 - Required password, 4 - Invalid password, 5 - External access denied]

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|


### Model StorageFilter
[0 - None, 1 - Internal, 2 - ThirdParty]

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|


### Model StringWrapper

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **response** | **String** |  | [optional] |
| **count** | **Integer** (int32) | The total number of items in the response | [optional] |
| **links** | [**List**](#model-booleanwrapperlinks-item) | List of links related to the response | [optional] |
| **status** | **Integer** (int32) | HTTP status code of the response | [optional] |
| **statusCode** | **Integer** (int32) | HTTP status code of the response (duplicate of status) | [optional] |


### Model SubjectType
[0 - User, 1 - External link, 2 - Group, 3 - Invitation link, 4 - Primary external link]

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|


### Model SubmitForm
The Complete &amp; Submit button settings.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **visible** | **Boolean** | Specifies whether the Complete  &amp; Submit button will be displayed or hidden on the top toolbar. | [optional] [example: true] |
| **resultMessage** | **String** | A message displayed after forms are submitted. | [optional] [example: Form submitted successfully] [nullable] |


### Model TemplatesConfig
The presence or absence of the templates in the Create New... menu option.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **image** | **String** | The absolute URL to the image for template. | [optional] [example: https://portal.example.com/templates/template1.png] [nullable] |
| **title** | **String** | The template title that will be displayed in the Create New... menu option. | [optional] [example: Blank Document] [nullable] |
| **url** | **URI** (uri) | The absolute URL to the document where it will be created and available after creation. | [optional] [example: https://portal.example.com/editor/new?template=blank] [nullable] |


### Model TemplatesRequestDto
The request parameters for adding files to the template list.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **fileIds** | **List** (int32) | The list of file IDs. | [optional] [example: [1,2,3]] [nullable] |


### Model ThirdPartyBackupRequestDto
The third-party backup request parameters.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **url** | **String** | The connection URL for the sharepoint. | [optional] [example: https://sharepoint.example.com] [nullable] |
| **login** | **String** | The login. | [optional] [example: admin] [nullable] |
| **password** | **String** | The password. | [optional] [example: P@ssw0rd] [nullable] |
| **token** | **String** | The authentication token. | [optional] [example: abc123def456] [nullable] |
| **customerTitle** | **String** | The customer title. | [optional] [example: My Cloud Storage] [nullable] |
| **providerKey** | **String** | The provider key. | [optional] [example: SharePoint] [nullable] |


### Model ThirdPartyParams
The third-party account parameters.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **auth\_data** | [**AuthData**](#model-authdata) |  | [optional] |
| **corporate** | **Boolean** | Specifies if this is a corporate account or not. | [optional] [example: false] |
| **roomsStorage** | **Boolean** | Specifies if this is a room storage or not. | [optional] [example: false] |
| **customer\_title** | **String** | The customer title. | [optional] [example: My Storage] [nullable] |
| **provider\_id** | **Integer** (int32) | The provider ID. | [optional] [example: 1] [nullable] |
| **provider\_key** | **String** | The provider key. | [optional] [example: GoogleDrive] [nullable] |


### Model ThirdPartyParamsArrayWrapper

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **response** | [**List**](#model-thirdpartyparams) |  | [optional] |
| **count** | **Integer** (int32) | The total number of items in the response | [optional] |
| **links** | [**List**](#model-booleanwrapperlinks-item) | List of links related to the response | [optional] |
| **status** | **Integer** (int32) | HTTP status code of the response | [optional] |
| **statusCode** | **Integer** (int32) | HTTP status code of the response (duplicate of status) | [optional] |


### Model ThirdPartyRequestDto
The third-party request parameters.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **url** | **String** | The connection URL for the sharepoint. | [optional] [example: https://example.com] [nullable] |
| **login** | **String** | The third-party request login. | [optional] [example: admin] [nullable] |
| **password** | **String** | The third-party request password. | [optional] [example: password123] [nullable] |
| **token** | **String** | The authentication token. | [optional] [example: abc123] [nullable] |
| **customerTitle** | **String** | The customer title. | [required] [example: My Document] [nullable] |
| **providerKey** | **String** | The provider key. | [required] [example: abc123] [nullable] |
| **providerId** | **Integer** (int32) | The provider ID. | [optional] [example: 1] [nullable] |


### Model Thumbnail
[0 - Waiting, 1 - Created, 2 - Error, 3 - Not required, 4 - Creating]

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|


### Model UpdateComment
The parameters for updating a comment.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **version** | **Integer** (int32) | The comment version. | [required] [example: 1] |
| **comment** | **String** | The comment text. | [optional] [example: This is a comment] [nullable] |


### Model UpdateFile
The parameters for updating a file.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **title** | **String** | The file title to update. | [optional] [example: My Document] [minLength: 0] [maxLength: 165] [nullable] |
| **lastVersion** | **Integer** (int32) | The number of the latest file version. | [optional] [example: 1] |


### Model UpdateRoomGroupRequest

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **roomsToAdd** | [**List**](#model-duplicaterequestdtofileids) | The list of room IDs to add to the group. | [optional] [example: [1,2,3]] [nullable] |
| **roomsToRemove** | [**List**](#model-duplicaterequestdtofileids) | The list of room IDs to remove from the group. | [optional] [example: [1,2,3]] [nullable] |
| **groupName** | **String** | The group name. | [optional] [example: New Group Name] [minLength: 0] [maxLength: 128] [nullable] |


### Model UpdateRoomRequest
The request parameters for updating a room.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **title** | **String** | The room title. | [optional] [example: My Document] [minLength: 0] [maxLength: 170] [nullable] |
| **quota** | **Long** (int64) | The room quota. | [optional] [example: 10485760] [nullable] |
| **indexing** | **Boolean** | Specifies whether to create a third-party room with indexing. | [optional] [example: true] [nullable] |
| **denyDownload** | **Boolean** | Specifies whether to deny downloads from the third-party room. | [optional] [example: true] [nullable] |
| **lifetime** | [**RoomDataLifetimeDto**](#model-roomdatalifetimedto) |  | [optional] |
| **watermark** | [**WatermarkRequestDto**](#model-watermarkrequestdto) |  | [optional] |
| **logo** | [**LogoRequest**](#model-logorequest) |  | [optional] |
| **tags** | **List** | The list of tags. | [optional] [example: ["tag1","tag2"]] [nullable] |
| **color** | **String** | The room color. | [optional] [example: #FF5733] [minLength: 0] [maxLength: 6] [nullable] |
| **cover** | **String** | The room cover. | [optional] [example: cover1] [minLength: 0] [maxLength: 50] [nullable] |
| **chatSettings** | [**ChatSettings**](#model-chatsettings) |  | [optional] |
| **sendFormToExternalDB** | **Boolean** | Specifies whether to send form data to external database. | [optional] [example: false] [nullable] |
| **saveFormAsXLSX** | **Boolean** | Specifies whether to save form data as XLSX file. | [optional] [example: false] [nullable] |


### Model UpdateRoomsQuotaRequestDtoInteger
The request parameters for updating the room quota.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **roomIds** | [**List**](#model-duplicaterequestdtofileids) | The list of room IDs. | [optional] [example: [1,2,3]] [nullable] |
| **quota** | **Long** (int64) | The room quota. | [optional] [example: 10485760] |


### Model UpdateRoomsRoomIdsRequestDtoInteger
The request parameters for updating the rooms.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **roomIds** | [**List**](#model-duplicaterequestdtofileids) | The list of room IDs. | [optional] [example: [1,2,3]] [nullable] |


### Model UpdateTagRequestDto
The request parameters for creating a tag.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **oldName** | **String** | The old tag name. | [required] [example: old-tag] [minLength: 0] [maxLength: 255] [nullable] |
| **newName** | **String** | The new tag name. | [required] [example: new-tag] [minLength: 0] [maxLength: 255] [nullable] |


### Model UploadResultDto
The upload result parameters.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **success** | **Boolean** | Specifies if the upload operation is successful or not. | [optional] [example: true] |
| **data** | **oas_any_type_not_mapped** | The uploaded data. | [optional] [example: {"id":10,"title":"document.docx"}] [nullable] |
| **message** | **String** | The message sent after the successful upload operation. | [optional] [example: File uploaded successfully] [nullable] |


### Model UploadResultWrapper

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **response** | [**UploadResultDto**](#model-uploadresultdto) |  | [optional] |
| **count** | **Integer** (int32) | The total number of items in the response | [optional] |
| **links** | [**List**](#model-booleanwrapperlinks-item) | List of links related to the response | [optional] |
| **status** | **Integer** (int32) | HTTP status code of the response | [optional] |
| **statusCode** | **Integer** (int32) | HTTP status code of the response (duplicate of status) | [optional] |


### Model UploadSessionResponseDtoInteger
The upload session response parameters.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **id** | **Integer** (int32) | The upload session ID. | [optional] [example: 1] |
| **folderId** | **Integer** (int32) | The folder ID where the file is being uploaded. | [optional] [example: 1] |
| **version** | **Integer** (int32) | The file version number. | [optional] [example: 1] |
| **title** | **String** | The file title. | [optional] [example: My Document.docx] [nullable] |
| **providerKey** | **String** | The third-party provider key. | [optional] [example: Google] [nullable] |
| **uploaded** | **Boolean** | Specifies whether the file has been uploaded. | [optional] [example: false] |
| **file** | [**FileDtoInteger**](#model-filedtointeger) |  | [optional] |


### Model UploadSessionResponseIntegerWrapper

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **response** | [**UploadSessionResponseDtoInteger**](#model-uploadsessionresponsedtointeger) |  | [optional] |
| **count** | **Integer** (int32) | The total number of items in the response | [optional] |
| **links** | [**List**](#model-booleanwrapperlinks-item) | List of links related to the response | [optional] |
| **status** | **Integer** (int32) | HTTP status code of the response | [optional] |
| **statusCode** | **Integer** (int32) | HTTP status code of the response (duplicate of status) | [optional] |


### Model UserConfig
The configuration parameters of the user currently viewing or editing the document.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **id** | **String** | The user ID. | [optional] [example: user_0001] [nullable] |
| **name** | **String** | The full name of the user. | [optional] [example: John Doe] [nullable] |
| **image** | **String** | The path to the user&#39;s avatar. | [optional] [example: https://portal.example.com/avatar/user_0001.png] [nullable] |
| **roles** | **List** | Roles | [optional] [example: ["admin","editor"]] [nullable] |
| **customerId** | **String** | Customer identifier associated with the user. | [optional] [example: cust_001] [nullable] |


### Model UserInfo
The user information.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **id** | **UUID** (uuid) | The user ID. | [optional] [example: 00000000-0000-0000-0000-000000000000] |
| **firstName** | **String** | The user&#39;s first name. | [optional] [example: John] [nullable] |
| **lastName** | **String** | The user&#39;s last name. | [optional] [example: Doe] [nullable] |
| **userName** | **String** | The user username. | [optional] [example: johndoe] [nullable] |
| **birthDate** | **Date** (date-time) | The user birthday. | [optional] [example: 1990-01-01T00:00:00Z] [nullable] |
| **sex** | **Boolean** | The user sex (male or female). | [optional] [example: true] [nullable] |
| **status** | [**EmployeeStatus**](#model-employeestatus) |  | [optional] [enum: 1, 2, 4, 5, 7] |
| **activationStatus** | [**EmployeeActivationStatus**](#model-employeeactivationstatus) |  | [optional] [enum: 0, 1, 2, 4] |
| **terminatedDate** | **Date** (date-time) | The date and time when the user account was terminated. | [optional] [example: 2025-12-31T23:59:59Z] [nullable] |
| **title** | **String** | The user title. | [optional] [example: Manager] [nullable] |
| **workFromDate** | **Date** (date-time) | The user registration date. | [optional] [example: 2020-01-15T00:00:00Z] [nullable] |
| **email** | **String** (email) | The user email address. | [optional] [example: john.doe@example.com] [nullable] |
| **contacts** | **String** | The list of user contacts in the string format. | [optional] [example: skype:johndoe\|telegram:@johndoe] [nullable] |
| **contactsList** | **List** | The list of user contacts. | [optional] [example: ["skype:johndoe","telegram:@johndoe"]] [nullable] |
| **location** | **String** | The user location. | [optional] [example: New York, USA] [nullable] |
| **notes** | **String** | The user notes. | [optional] [example: Additional information about the user] [nullable] |
| **removed** | **Boolean** | Specifies if the user account was removed or not. | [optional] [example: false] |
| **lastModified** | **Date** (date-time) | The date and time when the user account was last modified. | [optional] [example: 2025-02-08T10:30:00Z] |
| **tenantId** | **Integer** (int32) | The tenant ID. | [optional] [example: 1] |
| **isActive** | **Boolean** | Specifies if the user is active or not. | [optional] [example: true] |
| **cultureName** | **String** | The user culture code. | [optional] [example: en-US] [nullable] |
| **mobilePhone** | **String** | The user mobile phone. | [optional] [example: +1234567890] [nullable] |
| **mobilePhoneActivationStatus** | [**MobilePhoneActivationStatus**](#model-mobilephoneactivationstatus) |  | [optional] [enum: 0, 1] |
| **sid** | **String** | The LDAP user identifier. | [optional] [example: S-1-5-21-3623811015-3361044348-30300820-1013] [nullable] |
| **ldapQouta** | **Long** (int64) | The LDAP user quota attribute. | [optional] [example: 1073741824] |
| **ssoNameId** | **String** | The SSO SAML user identifier. | [optional] [example: johndoe@example.com] [nullable] |
| **ssoSessionId** | **String** | The SSO SAML user session identifier. | [optional] [example: _1a2b3c4d5e6f7g8h9i0j] [nullable] |
| **createDate** | **Date** (date-time) | The date and time when the user account was created. | [optional] [example: 2020-01-15T00:00:00Z] |
| **createdBy** | **UUID** (uuid) | The ID of the user who created the current user account. | [optional] [example: 00000000-0000-0000-0000-000000000000] [nullable] |
| **spam** | **Boolean** | Specifies if tips, updates and offers are allowed to be sent to the user or not. | [optional] [example: false] [nullable] |
| **checkActivation** | **Boolean** | Indicates whether the activation status of the employee or recipient is unchecked or inactive.  Depending on the context, this property evaluates the activation or eligibility status accordingly. | [optional] [example: false] |


### Model UserInvitation
The user invitation parameters.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **usersIds** | **List** (uuid) | The list of user IDs. | [optional] [example: ["00000000-0000-0000-0000-000000000000"]] [nullable] |
| **resendAll** | **Boolean** | Specifies whether to resend all user invitations or not. | [optional] [example: false] |


### Model VectorizationStatus
[0 - In Progress, 1 - Completed, 2 - Failed]

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|


### Model WatermarkAdditions
[1 - User name, 2 - User email, 4 - User ip adress, 8 - Current date, 16 - Room name]

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|


### Model WatermarkDto
The watermark settings.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **additions** | [**WatermarkAdditions**](#model-watermarkadditions) |  | [required] [enum: 1, 2, 4, 8, 16] |
| **text** | **String** | The watermark text. | [optional] [example: Confidential] [nullable] |
| **rotate** | **Integer** (int32) | The watermark text and image rotate. | [required] [example: 45] |
| **imageScale** | **Integer** (int32) | The watermark image scale. | [required] [example: 100] |
| **imageUrl** | **String** | The watermark image url. | [optional] [example: http://localhost/watermark.png] [nullable] |
| **imageHeight** | **Double** (double) | The watermark image height. | [required] [example: 100.0] |
| **imageWidth** | **Double** (double) | The watermark image width. | [required] [example: 200.0] |


### Model WatermarkOnDraw
The document watermark parameters.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **width** | **Double** (double) | Defines the watermark width measured in millimeters. | [optional] [example: 150] |
| **height** | **Double** (double) | Defines the watermark height measured in millimeters. | [optional] [example: 100] |
| **margins** | **List** (int32) | Defines the watermark margins measured in millimeters. | [optional] [example: [10,10,10,10]] [nullable] |
| **fill** | **String** | Defines the watermark fill color. | [optional] [example: #FF0000] [nullable] |
| **rotate** | **Integer** (int32) | Defines the watermark rotation angle. | [optional] [example: 45] |
| **transparent** | **Double** (double) | Defines the watermark transparency percentage. | [optional] [example: 0.4] |
| **paragraphs** | [**List**](#model-paragraph) | The list of paragraphs of the watermark. | [optional] [example: [{"align":2,"runs":[{"fill":[124,124,124],"text":"CONFIDENTIAL","fontSize":26}]}]] [nullable] |


### Model WatermarkRequestDto
The request parameters for adding watermarks.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **enabled** | **Boolean** | Specifies whether watermarks are on or off. | [optional] [example: true] [nullable] |
| **additions** | [**WatermarkAdditions**](#model-watermarkadditions) |  | [optional] [enum: 1, 2, 4, 8, 16] |
| **text** | **String** | The watermark text. | [optional] [example: Confidential] [minLength: 0] [maxLength: 255] [nullable] |
| **rotate** | **Integer** (int32) | The watermark text and image rotate angle. | [optional] [example: -45] |
| **imageScale** | **Integer** (int32) | The watermark image scale. | [optional] [example: 100] |
| **imageUrl** | **String** | The path to the temporary image file. | [optional] [example: /tmp/watermark.png] [nullable] |
| **imageHeight** | **Double** (double) | The watermark image height. | [optional] [example: 100.0] |
| **imageWidth** | **Double** (double) | The watermark image width. | [optional] [example: 200.0] |


### Model XlsxReportResponseDto
The XLSX report task response parameters.

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **form** | [**FileDtoInteger**](#model-filedtointeger) |  | [optional] |
| **task** | [**DocumentBuilderTaskDto**](#model-documentbuildertaskdto) |  | [optional] |
| **isNewFile** | **Boolean** | Specifies whether the XLSX report file is newly created or an existing file will be updated. | [optional] [example: true] |


### Model XlsxReportResponseWrapper

| Name | Type | Description | Notes |
|------------ | ------------- | ------------- | -------------|
| **response** | [**XlsxReportResponseDto**](#model-xlsxreportresponsedto) |  | [optional] |
| **count** | **Integer** (int32) | The total number of items in the response | [optional] |
| **links** | [**List**](#model-booleanwrapperlinks-item) | List of links related to the response | [optional] |
| **status** | **Integer** (int32) | HTTP status code of the response | [optional] |
| **statusCode** | **Integer** (int32) | HTTP status code of the response (duplicate of status) | [optional] |


## Authorization


### asc_auth_key
- **Type**: API key
- **API key parameter name**: asc_auth_key
- **Location**: 


### Basic

- **Type**: HTTP basic authentication


### Bearer

- **Type**: HTTP Bearer Token authentication (JWT)


### ApiKeyBearer
- **Type**: API key
- **API key parameter name**: ApiKeyBearer
- **Location**: HTTP header


### OAuth2

- **Type**: OAuth
- **Flow**: accessCode
- **Authorization URL**: 
- **Scopes**: 
  - read: Read access to protected resources
  - write: Write access to protected resources


### OpenId


### cookieAuth
- **Type**: API key
- **API key parameter name**: asc_auth_key
- **Location**: 


### bearerAuth

- **Type**: HTTP Bearer Token authentication


### x-signature
- **Type**: API key
- **API key parameter name**: x-signature
- **Location**: 

