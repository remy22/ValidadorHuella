using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace ValidadorHuella.DTO
{
    [Serializable]
    [XmlRoot("UsuariosList", Namespace = "")]
    public class UsuariosList
    {
        [XmlArray("Usuarios")]
        public Usuario[] Usuarios
        {
            get;
            set;
        }
    }
}
