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

namespace ASC.Web.Core.Users.Import;

public class TextFileUserImporter(Stream stream) : IUserImporter
{
    protected Dictionary<string, string> NameMapping { get; init; }

    protected IList<string> ExcludeList { get; private set; } = new List<string> { "ID", "Status" };


    public Encoding Encoding { get; set; } = Encoding.UTF8;

    public char Separator { get; set; } = ';';

    public bool HasHeader { get; set; }

    public string TextDelmiter { get; set; } = "\"";

    public string DefaultHeader { get; set; }


    public virtual IEnumerable<UserInfo> GetDiscoveredUsers()
    {
        var users = new List<UserInfo>();

        var fileLines = new List<string>();
        using (var reader = new StreamReader(stream, Encoding, true))
        {
            fileLines.AddRange(reader.ReadToEnd().Split([Environment.NewLine, "\n", "\r\n"], StringSplitOptions.RemoveEmptyEntries));
        }

        if (!string.IsNullOrEmpty(DefaultHeader))
        {
            fileLines.Insert(0, DefaultHeader);
        }
        if (0 < fileLines.Count)
        {
            var mappedProperties = new Dictionary<int, PropertyInfo>();
            //Get the map
            var infos = typeof(UserInfo).GetProperties(BindingFlags.Public | BindingFlags.Instance);
            var fieldsCount = GetFieldsMapping(fileLines[0], infos, mappedProperties);

            //Begin read file
            for (var i = 1; i < fileLines.Count; i++)
            {
                users.Add(GetExportedUser(fileLines[i], mappedProperties, fieldsCount));
            }
        }
        return users;
    }

    private UserInfo GetExportedUser(string line, Dictionary<int, PropertyInfo> mappedProperties, int fieldsCount)
    {
        var exportedUser = new UserInfo
        {
            Id = Guid.NewGuid()
        };

        var dataFields = GetDataFields(line);
        for (var j = 0; j < Math.Min(fieldsCount, dataFields.Length); j++)
        {
            //Get corresponding property
            var propinfo = mappedProperties[j];
            if (propinfo != null)
            {
                //Convert value
                var value = ConvertFromString(dataFields[j], propinfo.PropertyType);
                if (value != null)
                {
                    propinfo.SetValue(exportedUser, value, []);
                }
            }
        }
        return exportedUser;
    }

    private string[] GetDataFields(string line)
    {
        var pattern = $"{Separator}(?=(?:[^\"]*\"[^\"]*\")*(?![^\"]*\"))";
        var result = Regex.Split(line, pattern);

        //remove TextDelmiter
        result = Array.ConvertAll(result,
            original =>
            {
                if (original.StartsWith(TextDelmiter) && original.EndsWith(TextDelmiter))
                {
                    return original[1..^1];
                }

                return original;
            }
         );

        return result;
    }

    private int GetFieldsMapping(string firstLine, IEnumerable<PropertyInfo> infos, Dictionary<int, PropertyInfo> mappedProperties)
    {
        var fields = firstLine.Split([Separator], StringSplitOptions.RemoveEmptyEntries);
        for (var i = 0; i < fields.Length; i++)
        {
            var field = fields[i];
            //Find apropriate field in UserInfo
            foreach (var info in infos)
            {
                var propertyField = field.Trim();
                NameMapping?.TryGetValue(propertyField, out propertyField);
                if (!string.IsNullOrEmpty(propertyField) && !ExcludeList.Contains(propertyField) && propertyField.Equals(info.Name, StringComparison.OrdinalIgnoreCase))
                {
                    //Add to map
                    mappedProperties.Add(i, info);
                }
            }
            if (mappedProperties.TryAdd(i, null))
            {
                //No property was found
            }
        }
        return fields.Length;
    }

    private static object ConvertFromString(string value, Type type)
    {
        var converter = TypeDescriptor.GetConverter(type);
        return converter.CanConvertFrom(typeof(string)) ? converter.ConvertFromString(value) : null;
    }
}
