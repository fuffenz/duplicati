#region Disclaimer / License
// Copyright (C) 2008, Kenneth Skovhede
// http://www.hexad.dk, opensource@hexad.dk
// 
// This library is free software; you can redistribute it and/or
// modify it under the terms of the GNU Lesser General Public
// License as published by the Free Software Foundation; either
// version 2.1 of the License, or (at your option) any later version.
// 
// This library is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
// Lesser General Public License for more details.
// 
// You should have received a copy of the GNU Lesser General Public
// License along with this library; if not, write to the Free Software
// Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA
// 
#endregion
using System;
using System.Collections.Generic;
using System.Text;

namespace Duplicati.Library.Backend
{
    public class BackendLoader : IBackendInterface
    {
        private static object m_lock = new object();
        private static Dictionary<string, Type> m_backends;
        private IBackendInterface m_interface;

        public BackendLoader(string url, Dictionary<string, string> options)
            : this()
        {
            m_interface = GetBackend(url, options);
            if (m_interface == null)
                throw new ArgumentException("The supplied url is not supported");
        }

        public static string[] Backends
        {
            get
            {
                LoadBackends();
                return new List<string>(m_backends.Keys).ToArray();
            }
        }

        private static void LoadBackends()
        {
            if (m_backends == null)
                lock (m_lock)
                    if (m_backends == null)
                    {
                        Dictionary<string, Type> backends = new Dictionary<string, Type>();

                        string path = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                        List<string> files = new List<string>();
                        files.AddRange(System.IO.Directory.GetFiles(path, "*.dll"));

                        //We can override with the backends path
                        path = System.IO.Path.Combine(path, "backends");
                        if (System.IO.Directory.Exists(path))
                            files.AddRange(System.IO.Directory.GetFiles(path, "*.dll"));

                        foreach (string s in files)
                        {
                            try
                            {
                                System.Reflection.Assembly asm = System.Reflection.Assembly.LoadFile(s);
                                if (asm == System.Reflection.Assembly.GetExecutingAssembly())
                                    continue;

                                foreach (Type t in asm.GetExportedTypes())
                                    if (typeof(IBackendInterface).IsAssignableFrom(t) && t != typeof(IBackendInterface))
                                    {
                                        IBackendInterface i = Activator.CreateInstance(t) as IBackendInterface;
                                        backends[i.ProtocolKey] = t;
                                    }
                            }
                            catch
                            {
                            }
                        }

                        m_backends = backends;
                    }
        }

        public BackendLoader()
        {
            m_interface = null;
            LoadBackends();
        }

        public static IBackendInterface GetBackend(string url, Dictionary<string, string> options)
        {
            if (string.IsNullOrEmpty(url))
                throw new ArgumentNullException("index");

            string scheme = new Uri(url).Scheme.ToLower();

            if (m_backends == null)
                LoadBackends();

            if (m_backends.ContainsKey(scheme))
                return (IBackendInterface)Activator.CreateInstance(m_backends[scheme], url, options);
            else
                return null;
        }

        #region IBackendInterface Members

        public string DisplayName
        {
            get 
            {
                if (m_interface == null)
                    throw new Exception("This instance is not bound to a particular backend");
                else
                    return m_interface.DisplayName;
            }
        }

        public string ProtocolKey
        {
            get
            {
                if (m_interface == null)
                    throw new Exception("This instance is not bound to a particular backend");
                else
                    return m_interface.ProtocolKey;
            }
        }

        public List<FileEntry> List()
        {
            if (m_interface == null)
                throw new ArgumentException("This instance was not created with an URL");
            
            return m_interface.List();
        }

        public void Put(string remotename, string filename)
        {
            if (m_interface == null)
                throw new ArgumentException("This instance was not created with an URL");
            
            m_interface.Put(remotename, filename);
        }

        public void Get(string remotename, string filename)
        {
            if (m_interface == null)
                throw new ArgumentException("This instance was not created with an URL");

            m_interface.Get(remotename, filename);
        }

        public void Delete(string remotename)
        {
            if (m_interface == null)
                throw new ArgumentException("This instance was not created with an URL");

            m_interface.Delete(remotename);
        }

        #endregion
    }
}
