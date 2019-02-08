//
// MonoDevelop XML Editor
//
// Copyright (C) 2005 Matthew Ward
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Collections.Generic;

namespace MonoDevelop.Xml.Completion
{
	public interface IXmlSchemaCompletionDataCollection: IEnumerable<XmlSchemaCompletionProvider>
	{
		XmlSchemaCompletionProvider this [string namespaceUri] { get; }
		XmlCompletionData[] GetNamespaceCompletionData ();
		XmlSchemaCompletionProvider GetSchemaFromFileName (string fileName);
	}
	
	public class XmlSchemaCompletionDataCollection : List<XmlSchemaCompletionProvider>, IXmlSchemaCompletionDataCollection
	{
		public XmlSchemaCompletionProvider this [string namespaceUri] {
			get {
				foreach (XmlSchemaCompletionProvider item in this)
					if (item.NamespaceUri == namespaceUri)
						return item;
				return null;
			}
		}
		
		public XmlCompletionData[] GetNamespaceCompletionData ()
		{
			List<XmlCompletionData> completionItems = new List<XmlCompletionData> ();
			foreach (XmlSchemaCompletionProvider schema in this)
				completionItems.Add (new XmlCompletionData (schema.NamespaceUri, XmlCompletionData.DataType.NamespaceUri));
			return completionItems.ToArray ();
		}
		
		public XmlSchemaCompletionProvider GetSchemaFromFileName (string fileName)
		{
			foreach (XmlSchemaCompletionProvider schema in this)
				if (schema.FileName == fileName)
					return schema;
			return null;
		}
	}
	
	public class MergedXmlSchemaCompletionDataCollection : IXmlSchemaCompletionDataCollection
	{
		XmlSchemaCompletionDataCollection builtin;
		XmlSchemaCompletionDataCollection user;
		
		public MergedXmlSchemaCompletionDataCollection (
		    XmlSchemaCompletionDataCollection builtin,
		    XmlSchemaCompletionDataCollection user)
		{
			this.user = user;
			this.builtin = builtin;
		}
		
		public XmlSchemaCompletionProvider this [string namespaceUri] {
			get {
				XmlSchemaCompletionProvider val = user[namespaceUri];
				if (val == null)
					val = builtin[namespaceUri];
				return val;
			}
		}

		public XmlCompletionData[] GetNamespaceCompletionData ()
		{
			Dictionary <string, XmlCompletionData> items = new Dictionary<string,XmlCompletionData> ();
			foreach (XmlSchemaCompletionProvider schema in builtin)
				items[schema.NamespaceUri] = new XmlCompletionData (schema.NamespaceUri, XmlCompletionData.DataType.NamespaceUri);
			foreach (XmlSchemaCompletionProvider schema in user)
				items[schema.NamespaceUri] = new XmlCompletionData (schema.NamespaceUri, XmlCompletionData.DataType.NamespaceUri);
			XmlCompletionData[] result = new XmlCompletionData [items.Count];
			items.Values.CopyTo (result, 0);
			return result;
		}

		public XmlSchemaCompletionProvider GetSchemaFromFileName (string fileName)
		{
			XmlSchemaCompletionProvider data = user.GetSchemaFromFileName (fileName);
			if (data == null)
				data = builtin.GetSchemaFromFileName (fileName);
			return data;
		}
		
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator ()
		{
			return GetEnumerator ();
		}

		public IEnumerator<XmlSchemaCompletionProvider> GetEnumerator ()
		{
			foreach (XmlSchemaCompletionProvider x in builtin)
				if (user[x.NamespaceUri] == null)
					yield return x;
			foreach (XmlSchemaCompletionProvider x in user)
				yield return x;
		}
	}
}
