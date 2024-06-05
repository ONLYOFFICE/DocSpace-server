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

class SimpleStruct {
    constructor() {
        this.struct = {};
    }

    setStruct(path, uid, structDir) {
        if (!this.struct) {
            this.struct = {};
        }
        if (!this.struct[uid]) {
            this.struct[uid] = {};
        }
        this.struct[uid][path] = structDir;
        this.struct[uid].lastUpdate = new Date();
    }

    getStruct(path, uid) {
        return this.struct[uid] && this.struct[uid][path];
    }

    deleteStruct(uid) {
        delete this.struct[uid];
    }

    deleteStructs(uids) {
        uids.forEach(uid => {
            this.deleteStruct(uid);
        });
    }

    setFileObject(path, uid, newFile) {
        this.struct[uid][path].files.push(newFile);
        this.struct[uid].lastUpdate = new Date();
    }

    setFolderObject(path, uid, newFile) {
        this.struct[uid][path].folders.push(newFile);
        this.struct[uid].lastUpdate = new Date();
    }

    dropFileObject(Folder, uid, file) {
        this.struct[uid][Folder].files.forEach(el => {
            if (el.id == file.id) {
                const id = this.struct[uid][Folder].files.indexOf(el);
                this.struct[uid][Folder].files.splice(id, 1);
                this.struct[uid].lastUpdate = new Date();
            }
        });
    }

    dropFolderObject(Folder, uid, folder) {
        this.struct[uid][Folder].folders.forEach(el => {
            if (el.id == folder.id) {
                const id = this.struct[uid][Folder].folders.indexOf(el);
                this.struct[uid][Folder].folders.splice(id, 1);
                this.struct[uid].lastUpdate = new Date();
            }
        });
    }

    dropPath(path, uid) {
        if (this.struct[uid][path]) {
            delete this.struct[uid][path];
            this.struct[uid].lastUpdate = new Date();
        }
    }

    checkRename(elementFrom, elementTo, parentFolderFrom, parentFolderTo, user) {

        if (parentFolderFrom != parentFolderTo) return false;
        let elementFromIsExist = false;
        let elementToIsExist = false;
        let structFrom = this.struct[user.uid][parentFolderFrom];
        let structTo = this.struct[user.uid][parentFolderTo];
        structFrom.files.forEach((el) => {
            if (elementFrom == el.title) {
                elementFromIsExist = true;
            }
        });
        if (!elementFromIsExist) {
            structFrom.folders.forEach((el) => {
                if (elementFrom == el.title) {
                    elementFromIsExist = true;
                }
            });
        }
        if (!elementFromIsExist) return false;

        structTo.files.forEach((el) => {
            if (elementTo == el.title) {
                elementToIsExist = true;
            }
        });
        if (!elementToIsExist) {
            structTo.folders.forEach((el) => {
                if (elementTo == el.title) {
                    elementToIsExist = true;
                }
            });
        }
        if (!elementToIsExist) return true;
        return true;
    }

    renameFolderObject(element, newName, parentFolder, uid) {
        this.struct[uid][parentFolder].folders.forEach(el => {
            if (el.title == element) {
                const id = this.struct[uid][parentFolder].folders.indexOf(el);
                this.struct[uid][parentFolder].folders[id].title = newName;
                this.struct[uid].lastUpdate = new Date();
            }
        });
    }

    renameFileObject(element, newName, parentFolder, uid) {
        this.struct[uid][parentFolder].files.forEach(el => {
            if (el.title == element) {
                const id = this.struct[uid][parentFolder].files.indexOf(el);
                this.struct[uid][parentFolder].files[id].title = newName;
                this.struct[uid].lastUpdate = new Date();
            }
        });
    }

    structIsNotExpire(path, uid) {
        if (!this.struct[uid][path]) {
            return false;
        } else {
            const difference = 1000;
            const notExpire = (new Date() - this.struct[uid].lastUpdate) < difference;
            return notExpire;
        }
    }
}

module.exports = SimpleStruct;