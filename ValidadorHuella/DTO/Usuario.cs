using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace ValidadorHuella.DTO
{
    [Serializable]
    [XmlRoot("Usuario", Namespace = "")]
    public class Usuario
    {
        [XmlAttribute("Rut")]
        public string Rut
        {
            get;
            set;
        }

        [XmlElement("Nombre")]
        public string Nombre
        {
            get;
            set;
        }

        [XmlElement("Huella")]
        public byte[] Huella
        {
            get;
            set;
        }

        [XmlElement("TemplateBytes")]
        public byte[] TemplateBytes
        {
            get;
            set;
        }

        [XmlElement("TemplateSize")]
        public int TemplateSize
        {
            get;
            set;
        }
    }
}
