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

namespace ASC.Files.Core.Data;

[Scope(typeof(IDaoFactory))]
public class DaoFactory(IDaoFactory<string> daoFactoryThirdparty, IDaoFactory<int> daoFactoryInternal, IProviderDao providerDao) : IDaoFactory
{
    public IProviderDao ProviderDao { get; } = providerDao;

    public IFileDao<T> GetFileDao<T>()
    {
        if (typeof(T) == typeof(int))
        {
            return (IFileDao<T>)daoFactoryInternal.FileDao;
        }
        if (typeof(T) == typeof(string))
        {
            return (IFileDao<T>)daoFactoryThirdparty.FileDao;
        }
        return null;
    }

    public IFileDao<T> GetCacheFileDao<T>()
    {        
        if (typeof(T) == typeof(int))
        {
            return (IFileDao<T>)daoFactoryInternal.CacheFileDao ?? GetFileDao<T>();
        }
        if (typeof(T) == typeof(string))
        {
            return (IFileDao<T>)daoFactoryThirdparty.CacheFileDao ?? GetFileDao<T>();
        }
        return null;
    }

    public IFolderDao<T> GetFolderDao<T>()
    {
        if (typeof(T) == typeof(int))
        {
            return (IFolderDao<T>)daoFactoryInternal.FolderDao;
        }
        if (typeof(T) == typeof(string))
        {
            return (IFolderDao<T>)daoFactoryThirdparty.FolderDao;
        }
        return null;
    }

    public IFolderDao<T> GetCacheFolderDao<T>()
    {
        if (typeof(T) == typeof(int))
        {
            return (IFolderDao<T>)daoFactoryInternal.CacheFolderDao ?? GetFolderDao<T>();
        }
        if (typeof(T) == typeof(string))
        {
            return (IFolderDao<T>)daoFactoryThirdparty.CacheFolderDao ?? GetFolderDao<T>();
        }
        return null;
    }

    public ITagDao<T> GetTagDao<T>()
    {
        if (typeof(T) == typeof(int))
        {
            return (ITagDao<T>)daoFactoryInternal.TagDao;
        }
        if (typeof(T) == typeof(string))
        {
            return (ITagDao<T>)daoFactoryThirdparty.TagDao;
        }
        
        return null;
    }

    public ISecurityDao<T> GetSecurityDao<T>()
    {
        if (typeof(T) == typeof(int))
        {
            return (ISecurityDao<T>)daoFactoryInternal.SecurityDao;
        }
        if (typeof(T) == typeof(string))
        {
            return (ISecurityDao<T>)daoFactoryThirdparty.SecurityDao;
        }
        return null;
    }

    public ILinkDao<T> GetLinkDao<T>()
    {
        if (typeof(T) == typeof(int))
        {
            return (ILinkDao<T>)daoFactoryInternal.LinkDao;
        }
        if (typeof(T) == typeof(string))
        {
            return (ILinkDao<T>)daoFactoryThirdparty.LinkDao;
        }
        return null;
    }

    public IMappingId<T> GetMapping<T>()
    {
        if (typeof(T) == typeof(int))
        {
            return (IMappingId<T>)daoFactoryInternal.Mapping;
        }
        if (typeof(T) == typeof(string))
        {
            return (IMappingId<T>)daoFactoryThirdparty.Mapping;
        }
        return null;
    }
}

[Scope(typeof(IDaoFactory<int>), GenericArguments = [typeof(int)])]
[Scope(typeof(IDaoFactory<string>), GenericArguments = [typeof(string)])]
public class DaoFactory<T> : IDaoFactory<T>
{
    public DaoFactory(
        IFileDao<T> fileDao,
        IProviderDao providerDao,
        IFolderDao<T> folderDao,
        ICacheFolderDao<T> cacheFolderDao,
        ICacheFileDao<T> cacheFileDao,
        ITagDao<T> tagDao,
        ISecurityDao<T> securityDao,
        ILinkDao<T> linkDao,
        IMappingId<T> mapping)
    {
        ProviderDao = providerDao;
        FolderDao = folderDao;
        CacheFolderDao = cacheFolderDao;
        FileDao = fileDao;
        CacheFileDao = cacheFileDao;
        TagDao = tagDao;
        SecurityDao = securityDao;
        LinkDao = linkDao;
        Mapping = mapping;
    }
    
    public DaoFactory(
        IFileDao<T> fileDao,
        IProviderDao providerDao,
        IFolderDao<T> folderDao,
        ITagDao<T> tagDao,
        ISecurityDao<T> securityDao,
        ILinkDao<T> linkDao,
        IMappingId<T> mapping)
    {
        ProviderDao = providerDao;
        FolderDao = folderDao;
        CacheFolderDao = folderDao;
        FileDao = fileDao;
        CacheFileDao = fileDao;
        TagDao = tagDao;
        SecurityDao = securityDao;
        LinkDao = linkDao;
        Mapping = mapping;
    }

    public IProviderDao ProviderDao { get; }
    public IFolderDao<T> FolderDao { get; }
    public IFolderDao<T> CacheFolderDao { get; }
    public IFileDao<T> FileDao { get; }
    public IFileDao<T> CacheFileDao { get; }
    public ITagDao<T> TagDao { get; }
    public ISecurityDao<T> SecurityDao { get; }
    public ILinkDao<T> LinkDao { get; }
    public IMappingId<T> Mapping { get; }
}