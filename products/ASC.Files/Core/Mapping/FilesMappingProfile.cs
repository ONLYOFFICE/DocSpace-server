// (c) Copyright Ascensio System SIA 2009-2025
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

namespace ASC.Files.Core.Mapping;

public class FilesMapping : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<DbFile, File<int>>()
            .IgnoreNullValues(true)
            .Map(dst => dst.PureTitle, src => src.Title)
            .ConstructUsing(src => MapContext.Current.GetService<File<int>>());

        config.NewConfig<DbFileQuery, File<int>>()
            .AfterMapping((source, dest) =>
            {                
                source.File.Adapt(dest);
                var tenantDateTimeConverter = MapContext.Current.GetService<TenantDateTimeConverter>();
                if (tenantDateTimeConverter != null)
                {
                    dest.CreateOn = tenantDateTimeConverter.Convert(source.File.CreateOn);
                    dest.ModifiedOn = tenantDateTimeConverter.Convert(source.File.ModifiedOn);
                    dest.LastOpened = tenantDateTimeConverter.Convert(source.LastOpened);
                }
            })
            .Map(r => r.ShareRecord, f => f.SharedRecord.Adapt<FileShareRecord<int>>())
            .ConstructUsing(src =>  MapContext.Current.GetService<File<int>>());

        config.NewConfig<DbFolder, Folder<int>>().IgnoreNullValues(true);
        
        config.NewConfig<DbFolderQuery, Folder<int>>()              
            .AfterMapping((source, dest) =>
            {
                source.Folder.Adapt(dest, config);
                var tenantDateTimeConverter = MapContext.Current.GetService<TenantDateTimeConverter>();
                if (tenantDateTimeConverter != null)
                {
                    dest.CreateOn = tenantDateTimeConverter.Convert(source.Folder.CreateOn);
                    dest.ModifiedOn = tenantDateTimeConverter.Convert(source.Folder.ModifiedOn);
                }

                MapContext.Current.GetService<FilesMappingAction>().Process(dest);
            })
            .ConstructUsing(src => MapContext.Current.GetService<Folder<int>>());

        config.NewConfig<FileShareRecord<int>, DbFilesSecurity>()
            .Map(dest => dest.EntryId, src => src.EntryId.ToString())
            .Map(dest => dest.TimeStamp, _ => DateTime.UtcNow)
            .BeforeMapping((source, dest) => MapContext.Current.GetService<FilesMappingAction>().Process(source, dest));
        
        config.NewConfig<FileShareRecord<string>, DbFilesSecurity>()
            .Map(dest => dest.TimeStamp, _ => DateTime.UtcNow)
            .BeforeMapping((source, dest) => MapContext.Current.GetService<FilesMappingAction>().Process(source, dest));
        
        config.NewConfig<DbFilesSecurity, FileShareRecord<int>>()
            .Map(dest => dest.EntryId, src => Convert.ToInt32(src.EntryId));
        
        config.NewConfig<DbFilesSecurity, FileShareRecord<string>>();
        
        config.NewConfig<SecurityTreeRecord, FileShareRecord<int>>()
            .Map(dest => dest.EntryId, src => Convert.ToInt32(src.EntryId));
        
        config.NewConfig<SecurityTreeRecord, FileShareRecord<string>>();

        config.NewConfig<DbFilesFormRoleMapping, FormRole>();

        config.NewConfig<RoomDataLifetime, DbRoomDataLifetime>().TwoWays();
        config.NewConfig<WatermarkSettings, DbRoomWatermark>().TwoWays();
    }
}
