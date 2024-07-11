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

const {
    Duplex
} = require('stream');
const {
    maxChunkSize
} = require('../server/config.js');
const FormData = require("form-data");
const {
    chunkedUploader,
    rewritingFile
} = require('../server/requestAPI.js');

class streamWrite extends Duplex {
    constructor(contents, arrayBufLength, ctx, location, user, file, createdNow) {
        super(null);
        this.contents = contents;
        this.contentsLength = 0;
        this.location = location;
        this.arrayBuf = arrayBufLength;
        this.firstPosition = 0;
        this.lastChunk = 0;
        this.count = -1;
        this.totalCount = 0;
        this.ctx = ctx;
        this.user = user;
        this.file = file;
        this.createdNow = createdNow;
    }

    _read() {
        for (let i = 0; i < this.contents.length; i++) {
            this.push(this.contents[i]);
        }
        this.push(null);
    }

    async _write(chunk, encoding, callback) {
        if (this.file) {
            if (!this.createdNow) {
                this.contents.push(chunk);
                this.contentsLength += chunk.length;
                if (this.contentsLength == this.ctx.estimatedSize) {
                    const form_data = new FormData();
                    form_data.append("FileExtension", this.file.fileExst);
                    form_data.append("DownloadUri", "");
                    form_data.append("Stream", this, {
                        filename: this.file.realTitle,
                        contentType: "text/plain"
                    });
                    form_data.append("Doc", "");
                    form_data.append("Forcesave", 'false');
                    await rewritingFile(this.ctx, this.file.id, form_data, this.user.token);
                }

            } else {
                for (let i = 0; chunk.length > i; i++) {
                    this.count++;
                    this.totalCount++;
                    this.arrayBuf[this.count] = chunk[i];

                    let fullChunk = this.count == (maxChunkSize - 1);
                    //let lastByte = chunk[i + 1] == undefined && (this.lastChunk > chunk.length || this.count == this.ctx.estimatedSize - 1);
                    let lastByte = this.totalCount == this.ctx.estimatedSize;

                    if (fullChunk || lastByte) {
                        this.arrayBuf.length = this.count + 1;
                        const form_data = new FormData();
                        form_data.append("files[]", Buffer.from(this.arrayBuf), "chunk" + i);
                        await chunkedUploader(this.ctx, this.location, form_data, this.user.token, this.ctx.estimatedSize, this.firstPosition, this.firstPosition + this.arrayBuf.length - 1);
                        this.firstPosition += this.arrayBuf.length;
                        if (this.ctx.estimatedSize < maxChunkSize) {
                            this.arrayBuf = [];
                        }
                        this.count = -1;
                    }
                    if (lastByte) {
                        if (global.gc) {
                            global.gc();
                        }
                        this.arrayBuf = null;
                        this.ctx = null;
                    }
                }
                this.lastChunk = chunk.length;
            }
        }
        callback(null);
    }
}

module.exports = streamWrite;