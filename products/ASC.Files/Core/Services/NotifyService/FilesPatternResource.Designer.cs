﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace ASC.Files.Core.Services.NotifyService {
    using System;
    
    
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "17.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    public class FilesPatternResource {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal FilesPatternResource() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        public static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("ASC.Files.Core.Services.NotifyService.FilesPatternResource", typeof(FilesPatternResource).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   Overrides the current thread's CurrentUICulture property for all
        ///   resource lookups using this strongly typed resource class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        public static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Check ready forms.
        /// </summary>
        public static string button_CheckReadyForms {
            get {
                return ResourceManager.GetString("button_CheckReadyForms", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to A new document &quot;$DocumentTitle&quot; has been created in the room &quot;$RoomTitle&quot;.
        /// </summary>
        public static string pattern_DocumentCreatedInRoom_push {
            get {
                return ResourceManager.GetString("pattern_DocumentCreatedInRoom_push", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to New files have been uploaded to the room &quot;$RoomTitle&quot; ($Count).
        /// </summary>
        public static string pattern_DocumentsUploadedTo_push {
            get {
                return ResourceManager.GetString("pattern_DocumentsUploadedTo_push", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to A new file &quot;$DocumentTitle&quot; has been uploaded to the room &quot;$RoomTitle&quot;.
        /// </summary>
        public static string pattern_DocumentUploadedTo_push {
            get {
                return ResourceManager.GetString("pattern_DocumentUploadedTo_push", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to h1. All signers completed $DocumentTitle
        ///
        ///This is an email notification to inform you that all signers completed &quot;$DocumentTitle&quot;:&quot;$DocumentURL&quot;..
        /// </summary>
        public static string pattern_DocuSignComplete {
            get {
                return ResourceManager.GetString("pattern_DocuSignComplete", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to h1. $Message: $DocumentTitle
        ///
        ///Sign in to your DocuSign account for more information..
        /// </summary>
        public static string pattern_DocuSignStatus {
            get {
                return ResourceManager.GetString("pattern_DocuSignStatus", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to h1. $__AuthorName mentioned you in the document comment
        ///
        ///$__AuthorName mentioned you in the comment to the &quot;$DocumentTitle&quot;:&quot;$DocumentURL&quot; document in the room &quot;$RoomTitle&quot;:&quot;$RoomURL&quot;:
        ///
        ///$Message.
        /// </summary>
        public static string pattern_EditorMentions {
            get {
                return ResourceManager.GetString("pattern_EditorMentions", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to $ToUserName mentioned you in the document $DocumentTitle.
        /// </summary>
        public static string pattern_EditorMentions_push {
            get {
                return ResourceManager.GetString("pattern_EditorMentions_push", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to A new folder &quot;$FolderTitle&quot; has been created to the room &quot;$RoomTitle&quot;.
        /// </summary>
        public static string pattern_FolderCreatedInRoom_push {
            get {
                return ResourceManager.GetString("pattern_FolderCreatedInRoom_push", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to h1. Form $Message is filled out
        ///
        ///A new form &quot;$DocumentTitle&quot;:&quot;$DocumentURL&quot; is filled out in the room &quot;$RoomTitle&quot;:&quot;$RoomURL&quot;
        ///
        ///Form submitter: &quot;$FromUserName&quot;:&quot;$FromUserLink&quot;
        ///
        ///$OrangeButton.
        /// </summary>
        public static string pattern_FormReceived {
            get {
                return ResourceManager.GetString("pattern_FormReceived", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to 
        ///      Activity notification. &quot;$FromUserName&quot;:&quot;$FromUserLink&quot; started the form filling process:  &quot;$DocumentTitle&quot;:&quot;$DocumentURL&quot; in the room: &quot;$RoomTitle&quot;:&quot;$RoomURL&quot; — $__DateTime.
        ///    .
        /// </summary>
        public static string pattern_FormStartedFilling {
            get {
                return ResourceManager.GetString("pattern_FormStartedFilling", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to h1. You filled out the form $Message
        ///
        ///You filled out the form &quot;$DocumentTitle&quot;:&quot;$DocumentURL&quot; in the room &quot;$RoomTitle&quot;:&quot;$RoomURL&quot;
        ///
        ///Manager: &quot;$ToUserName&quot;:&quot;$ToUserLink&quot;
        ///
        ///$OrangeButton.
        /// </summary>
        public static string pattern_FormSubmitted {
            get {
                return ResourceManager.GetString("pattern_FormSubmitted", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Form &quot;$DocumentTitle&quot; has been filled out in the room &quot;$RoomTitle&quot;.
        /// </summary>
        public static string pattern_FormSubmitted_push {
            get {
                return ResourceManager.GetString("pattern_FormSubmitted_push", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to 
        ///      Activity notification. The form &quot;$DocumentTitle&quot;:&quot;$DocumentURL&quot; was completely filled out in the room: &quot;$RoomTitle&quot;:&quot;$RoomURL&quot; — $__DateTime.
        ///    .
        /// </summary>
        public static string pattern_FormWasCompletelyFilled {
            get {
                return ResourceManager.GetString("pattern_FormWasCompletelyFilled", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to You have been invited to the room &quot;$RoomTitle&quot;.
        /// </summary>
        public static string pattern_InvitedToRoom_push {
            get {
                return ResourceManager.GetString("pattern_InvitedToRoom_push", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to h1. Mailing completed
        ///
        ///This is an email notification to inform you that you have requested the mailing of $MailsCount messages and the process is now complete. $Message
        ///
        ///The successfully sent mail messages can be found in your &quot;Sent&quot;:&quot;$__VirtualRootPath/addons/mail/#sent&quot; folder of the Mail module..
        /// </summary>
        public static string pattern_MailMergeEnd {
            get {
                return ResourceManager.GetString("pattern_MailMergeEnd", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to $FromUserName moved the room &quot;$RoomTitle&quot; to the archive.
        /// </summary>
        public static string pattern_RoomMovedArchive_push {
            get {
                return ResourceManager.GetString("pattern_RoomMovedArchive_push", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to h1. $__AuthorName deleted a room
        ///
        ///$__AuthorName deleted the room: $RoomTitle.
        /// </summary>
        public static string pattern_RoomRemoved {
            get {
                return ResourceManager.GetString("pattern_RoomRemoved", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Your role in the room &quot;$RoomTitle&quot; has been changed to &quot;$RoomRole&quot;.
        /// </summary>
        public static string pattern_RoomUpdateAccessForUser_push {
            get {
                return ResourceManager.GetString("pattern_RoomUpdateAccessForUser_push", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to h1. Access granted to document: &quot;$DocumentTitle&quot;:&quot;$DocumentURL&quot;
        ///
        ///$__DateTime &quot;$__AuthorName&quot;:&quot;$__AuthorUrl&quot; granted you the access to the &quot;$DocumentTitle&quot;:&quot;$DocumentURL&quot; document with the following access rights: &quot;$AccessRights&quot;. 
        ///
        ///$Message.
        /// </summary>
        public static string pattern_ShareDocument {
            get {
                return ResourceManager.GetString("pattern_ShareDocument", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to h1. Access granted to folder: &quot;$DocumentTitle&quot;:&quot;$DocumentURL&quot;
        ///
        ///$__DateTime &quot;$__AuthorName&quot;:&quot;$__AuthorUrl&quot; granted you the access to the &quot;$DocumentTitle&quot;:&quot;$DocumentURL&quot; folder with the following access rights: &quot;$AccessRights&quot;. 
        ///
        ///$Message.
        /// </summary>
        public static string pattern_ShareFolder {
            get {
                return ResourceManager.GetString("pattern_ShareFolder", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to 
        ///      Activity notification. &quot;$FromUserName&quot;:&quot;$FromUserLink&quot; stopped form filling: &quot;$DocumentTitle&quot;:&quot;$DocumentURL&quot; in the room: &quot;$RoomTitle&quot;:&quot;$RoomURL&quot; — $__DateTime.
        ///    .
        /// </summary>
        public static string pattern_StoppedFormFilling {
            get {
                return ResourceManager.GetString("pattern_StoppedFormFilling", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to 
        ///      Activity notification. It’s your turn! Form filling is required: &quot;$DocumentTitle&quot;:&quot;$DocumentURL&quot; in the room: &quot;$RoomTitle&quot;:&quot;$RoomURL&quot; — $__DateTime.
        ///    .
        /// </summary>
        public static string pattern_YourTurnFormFilling {
            get {
                return ResourceManager.GetString("pattern_YourTurnFormFilling", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to &lt;patterns&gt;
        ///  &lt;formatter type=&quot;ASC.Notify.Patterns.NVelocityPatternFormatter, ASC.Core.Common&quot; /&gt;
        ///
        ///  &lt;pattern id=&quot;DocuSignComplete&quot; sender=&quot;email.sender&quot;&gt;
        ///    &lt;subject resource=&quot;|subject_DocuSignComplete|ASC.Files.Core.Services.NotifyService.FilesPatternResource,ASC.Files.Core&quot; /&gt;
        ///    &lt;body styler=&quot;ASC.Notify.Textile.TextileStyler,ASC.Core.Common&quot; resource=&quot;|pattern_DocuSignComplete|ASC.Files.Core.Services.NotifyService.FilesPatternResource,ASC.Files.Core&quot; /&gt;
        ///  &lt;/pattern&gt;
        ///  &lt;pattern id=&quot;DocuSignComple [rest of string was truncated]&quot;;.
        /// </summary>
        public static string patterns {
            get {
                return ResourceManager.GetString("patterns", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Documents. All signers completed $DocumentTitle.
        /// </summary>
        public static string subject_DocuSignComplete {
            get {
                return ResourceManager.GetString("subject_DocuSignComplete", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to All signers completed $DocumentTitle.
        /// </summary>
        public static string subject_DocuSignComplete_push {
            get {
                return ResourceManager.GetString("subject_DocuSignComplete_push", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Documents. All signers completed [$DocumentTitle]($DocumentURL).
        /// </summary>
        public static string subject_DocuSignComplete_tg {
            get {
                return ResourceManager.GetString("subject_DocuSignComplete_tg", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Documents. Sign status changed.
        /// </summary>
        public static string subject_DocuSignStatus {
            get {
                return ResourceManager.GetString("subject_DocuSignStatus", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Sign status changed.
        /// </summary>
        public static string subject_DocuSignStatus_push {
            get {
                return ResourceManager.GetString("subject_DocuSignStatus_push", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to DocSpace notification. Mentioned in document.
        /// </summary>
        public static string subject_EditorMentions {
            get {
                return ResourceManager.GetString("subject_EditorMentions", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to DocSpace notification. The form is filled out.
        /// </summary>
        public static string subject_FormReceived {
            get {
                return ResourceManager.GetString("subject_FormReceived", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to DocSpace notification..
        /// </summary>
        public static string subject_FormStartedFilling {
            get {
                return ResourceManager.GetString("subject_FormStartedFilling", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to DocSpace notification. The form is filled out.
        /// </summary>
        public static string subject_FormSubmitted {
            get {
                return ResourceManager.GetString("subject_FormSubmitted", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to DocSpace notification..
        /// </summary>
        public static string subject_FormWasCompletelyFilled {
            get {
                return ResourceManager.GetString("subject_FormWasCompletelyFilled", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Documents. Mailing is complete..
        /// </summary>
        public static string subject_MailMergeEnd {
            get {
                return ResourceManager.GetString("subject_MailMergeEnd", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Mailing is complete..
        /// </summary>
        public static string subject_MailMergeEnd_push {
            get {
                return ResourceManager.GetString("subject_MailMergeEnd_push", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to DocSpace notification. Room deleted.
        /// </summary>
        public static string subject_RoomRemoved {
            get {
                return ResourceManager.GetString("subject_RoomRemoved", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Documents. Access granted to document: $DocumentTitle.
        /// </summary>
        public static string subject_ShareDocument {
            get {
                return ResourceManager.GetString("subject_ShareDocument", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Access granted to document: $DocumentTitle.
        /// </summary>
        public static string subject_ShareDocument_push {
            get {
                return ResourceManager.GetString("subject_ShareDocument_push", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Documents. Access granted to document: [$DocumentTitle]($DocumentURL).
        /// </summary>
        public static string subject_ShareDocument_tg {
            get {
                return ResourceManager.GetString("subject_ShareDocument_tg", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Access granted to document: $DocumentTitle.
        /// </summary>
        public static string subject_ShareEncryptedDocument_push {
            get {
                return ResourceManager.GetString("subject_ShareEncryptedDocument_push", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Documents. Access granted to folder: $DocumentTitle.
        /// </summary>
        public static string subject_ShareFolder {
            get {
                return ResourceManager.GetString("subject_ShareFolder", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Access granted to folder: $DocumentTitle.
        /// </summary>
        public static string subject_ShareFolder_push {
            get {
                return ResourceManager.GetString("subject_ShareFolder_push", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Documents. Access granted to folder: [$DocumentTitle]($DocumentURL).
        /// </summary>
        public static string subject_ShareFolder_tg {
            get {
                return ResourceManager.GetString("subject_ShareFolder_tg", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to DocSpace notification..
        /// </summary>
        public static string subject_StoppedFormFilling {
            get {
                return ResourceManager.GetString("subject_StoppedFormFilling", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to DocSpace notification..
        /// </summary>
        public static string subject_YourTurnFormFilling {
            get {
                return ResourceManager.GetString("subject_YourTurnFormFilling", resourceCulture);
            }
        }
    }
}
