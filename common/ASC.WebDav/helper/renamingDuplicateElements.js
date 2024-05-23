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

var addRealTitle = function (response, folderId) {
    if (folderId != '@root') {
        let structFile = response.data.response.files;
        for (let i = 0; i < structFile.length; i++) {
            response.data.response.files[i]['realTitle'] = structFile[i].title;
        }
        let structFolder = response.data.response.folders;
        for (let i = 0; i < structFolder.length; i++) {
            response.data.response.folders[i]['realTitle'] = structFolder[i].title;
        }
        return response;
    } else {
        return response;
    }
};

var checkDuplicateNames = function (response) {
    let structFile = response.data.response.files;
    for (let i = 0; i < structFile.length; i++) {
        for (let j = i; j < structFile.length; j++) {
            if ((i != j) && (structFile[i].title == structFile[j].title)) {
                return false;
            }
        }
    }
    let structFolder = response.data.response.folders;
    for (let i = 0; i < structFolder.length; i++) {
        for (let j = i; j < structFolder.length; j++) {
            if ((i != j) && (structFolder[i].title == structFolder[j].title)) {
                return false;
            }
        }
    }
    return true;
};



var localRename = function (response, folderId) {
    if (folderId != '@root') {
        if (!checkDuplicateNames(response)) {
            let structFile = response.data.response.files;
            for (let i = 0; i < structFile.length; i++) {
                let c = 1;
                for (let j = i; j < structFile.length; j++) {
                    if ((i != j) && (structFile[i].title == structFile[j].title)) {
                        const title = structFile[j].title;
                        const splitedTitle = title.split(".");
                        const realTitle = structFile[j].realTitle;
                        if (realTitle == title) {
                            response.data.response.files[j].title = splitedTitle[0] + `(${c}).` + splitedTitle[1];
                            c++;
                        } else {
                            let reversTitle = title.split("").reverse().join("");
                            let num = reversTitle.split(")", 2)[1].split("(")[0].split("").reverse().join("");
                            response.data.response.files[j].title = realTitle.split(".")[0] + `(${Number(num)+1}).` + splitedTitle[1];
                        }
                    }
                }
            }
            let structFolders = response.data.response.folders;
            for (let i = 0; i < structFolders.length; i++) {
                let c = 1;
                for (let j = i; j < structFolders.length; j++) {
                    if ((i != j) && (structFolders[i].title == structFolders[j].title)) {
                        const title = structFolders[j].title;
                        const realTitle = structFolders[j].realTitle;
                        if (realTitle == title) {
                            response.data.response.folders[j].title = title + `(${c})`;
                            c++;
                        } else {
                            let reversTitle = title.split("").reverse().join("");
                            let num = reversTitle.split(")", 2)[1].split("(")[0].split("").reverse().join("");
                            response.data.response.folders[j].title = realTitle.split(".")[0] + `(${Number(num)+1})`;
                        }
                    }
                }
            }
            return localRename(response, folderId);
        } else {
            return response;
        }
    } else {
        return response;
    }
};

module.exports = {
    addRealTitle,
    checkDuplicateNames,
    localRename
};