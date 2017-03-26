using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;

namespace CsvBinding
{
    public class BindableCsv:ObservableCollection<BindableCsvLine>
    {
        public BindableCsv(StringReader stringReader)
        {
            var parser = new CsvHelper.CsvReader(stringReader);
            parser.Configuration.HasHeaderRecord = true;
            parser.ReadHeader();

            while (parser.Read())
            {
                this.Add(new BindableCsvLine(parser.FieldHeaders,parser.CurrentRecord));
            }
        }
    }

    public class BindableCsvLine : DynamicObject, INotifyPropertyChanged
    {
        private Dictionary<string, StringTypeObjectPair> contents=new Dictionary<string, StringTypeObjectPair>();

        public class StringTypeObjectPair
        {
            public StringTypeObjectPair(string text)
            {
                SetByText(text);
            }

            public void SetByText(string text)
            {
                this.Text = text;
                Type t;
                Object = GuessFromString(text, out t);
                this.Type = t;
            }

            public String Text;
            public Type Type;
            public object Object;
        }

        public BindableCsvLine(string[] header,string[] record)
        {
            var doubleField=new Dictionary<string, int>();
            for (int i = 0; i < record.Length; i++)
            {
                string value = record[i];

                var fieldName = header.Length > i ? "field" : header[i];
                if (doubleField.ContainsKey(fieldName))
                {
                    int cnt = doubleField[fieldName] + 1;

                    string tempField = GetArrayName(fieldName, cnt);
                    while (doubleField.ContainsKey(tempField))
                    {
                        cnt++;
                        tempField = GetArrayName(fieldName, cnt);
                    }
                }
                if (doubleField.ContainsKey(fieldName))
                {
                    doubleField[fieldName]++;
                }
                else
                {
                    doubleField.Add(fieldName,1);
                }

                contents.Add(fieldName, new StringTypeObjectPair(value));
            }
        }

        private static string GetArrayName(string title, int cnt) => title + "[" + cnt + "]";

        public static object GuessFromString(string value,out Type t)
        {
            if (value.ToLower() == "null")
            {
                t = null;
                return null;
            }
            bool t1;
            if (bool.TryParse(value, out t1))
            {
                t = typeof(bool);
                return t1;
            }
            double t2;
            if (double.TryParse(value, out t2))
            {
                t = typeof(double);
                return t2;
            }
            t= typeof(string);
            return value;
        }

        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            var temp = GetResult(binder.Name, binder.IgnoreCase);
            if (temp==null)
            {
                return false;
            }
            temp.SetByText(value.ToString());
            OnPropertyChanged(binder.Name);
            return true;
        }

        public override IEnumerable<string> GetDynamicMemberNames()
        {
            return contents.Keys;
        }

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            var temp = GetResult(binder.Name, binder.IgnoreCase);
            if (temp==null)
            {
                result = null;
                return false;
            }

            if (binder.ReturnType == typeof(string))
            {
                result = temp.Text;
            }
            else
            {
                result = temp.Object;
            }
            return true;
        }

        private StringTypeObjectPair GetResult(string name, bool ignoreCase)
        {
            if (contents.ContainsKey(name))
            {
                return contents[name];
            }
            if (ignoreCase)
            {
                var w = contents.Where((a) => a.Key.ToLower() == name.ToLower());
                if (w.Any())
                {
                    return w.First().Value;
                }
            }
            return null;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
