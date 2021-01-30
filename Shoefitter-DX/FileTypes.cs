using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Media;

namespace ShoefitterDX
{
    public class FileType
    {
        public Type EditorType { get; }
        public ImageSource IconSource { get; }
        public Regex PathMatchExpression { get; }

        public FileType(Type editorType, ImageSource iconSource, Regex pathMatchExpression)
        {
            this.EditorType = editorType;
            this.IconSource = iconSource;
            this.PathMatchExpression = pathMatchExpression;
        }
    }

    public static class FileTypes
    {
        private static List<FileType> _allFileTypes = null;
        private static List<FileType> AllFileTypes
        {
            get
            {
                if (_allFileTypes == null)
                {
                    _allFileTypes = new List<FileType>
                    {
                        new FileType(typeof(Editors.CharacterEditor), null, new Regex(@".*\\blockfiles\\characters\\\w\w\w\w\\?$", RegexOptions.Compiled)), // Character directories
                        new FileType(typeof(Editors.AreaEditor), null, new Regex(@".*\\blockfiles\\levels\\\w\w\w\w\\\w\w\w\w\\?$", RegexOptions.Compiled)) // Area directories
                    };
                }
                return _allFileTypes;
            }
        }

        public static FileType DetermineType(string path)
        {
            return AllFileTypes.FirstOrDefault(type => type.PathMatchExpression.IsMatch(path));
        }
    }
}
