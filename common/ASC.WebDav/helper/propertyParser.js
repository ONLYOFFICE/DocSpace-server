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

class PropertyParser {
    static parsePath(path) {
        let pathArray = path.split('/');
        let targetElement = pathArray.pop();
        let targetElementArray = targetElement.split('.');
        if (targetElementArray.length > 1) {
            let ext = targetElementArray.pop();
            targetElementArray.push(ext.toLowerCase());
            targetElement = targetElementArray.join('.');
        }
        if (pathArray.length <= 1) {
            pathArray[0] = '/';
        }
        let parentPath = pathArray.join('/');
        return {
            element: targetElement,
            parentFolder: parentPath
        };
    }

    static parsePathTo(pathTo) {
        let pathArray = pathTo.split('/');
        if (pathArray[pathArray.length - 1] == '' && pathTo !== '/') {
            pathArray.pop();
            var newPath = pathArray.join('/');
        } else {
            var newPath = pathTo;
        }
        return newPath;
    }

    static parseDate(dateString) {
        let dateArray = dateString.split('.');
        dateArray = dateArray[0].split('T');
        let date = dateArray[0].split('-');
        let time = dateArray[1].split(':');
        return new Date(date[0], date[1] - 1, date[2], time[0], time[1], time[2]);
    }

    static parseFileExt(fileName) {
        let fileNameArray = (fileName || '').split('.');
        return fileNameArray.length > 1 ? fileNameArray.pop() : null;
    }
}

module.exports = PropertyParser;