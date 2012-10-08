using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using ValidadorHuella;
using ValidadorHuella.DTO;
using System.IO;
using System.Xml;
using System.Data.Linq;
using System.Linq;
using ValidadorHuella.Utils;

namespace ValidadorHuella
{
    public partial class IngresarUsuario : Form, DPFP.Capture.EventHandler
    {
        #region Propiedades Privadas
        delegate void Function();
        private DPFP.Capture.Capture Capturer;
        private DPFP.Processing.Enrollment Enroller;
        private DPFP.Sample MemSample;
        private DPFP.Template MemTemplate;
        #endregion

        #region Constructores
        public IngresarUsuario()
        {
            try
            {
                InitializeComponent();
                Enroller = new DPFP.Processing.Enrollment();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Validador Huella", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        #endregion

        #region Eventos del Formulario
        private void btnLeerHuella_Click(object sender, EventArgs e)
        {
            try
            {
                Capturer = new DPFP.Capture.Capture();				// Create a capture operation.

                if (null != Capturer)
                {
                    Capturer.EventHandler = this;
                    //Enroller = new DPFP.Processing.Enrollment();
                    if (null != Capturer)
                    {
                        try
                        {
                            Capturer.StartCapture();
                            SetStatus("Ponga su dedo indice en el huellero");
                        }
                        catch
                        {
                            MakeReport("No se pudo iniciar la captura!");
                        }
                    }
                }
                else
                {
                    MakeReport("No se pudo iniciar la operación de captura!");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Validador Huella", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnVolver_Click(object sender, EventArgs e)
        {
            if(Capturer != null)
                Capturer.StopCapture();
            this.Close();
        }

        private void btnIngresar_Click(object sender, EventArgs e)
        {
            try
            {
                XmlSerial serial = new XmlSerial();
                
                Usuario usuario = new Usuario();
                usuario.Rut = this.txtRut.Text;
                usuario.Nombre = this.txtNombre.Text;
                usuario.Huella = Funciones.ImageToByteArray(this.picHuella.Image);
                usuario.TemplateBytes = MemTemplate.Bytes;
                usuario.TemplateSize = MemTemplate.Size;

                String XmlizedString = serial.SerializeObjeto(usuario);  //Funciones.SerializeObject(usuario);

                XmlDocument xmlDoc = new XmlDocument();
                string path = Path.GetFullPath("DB/UsuarioRepository.xml");

                xmlDoc.Load(path);
                //(UsuariosList)Funciones.DeserializeObject<UsuariosList>(xmlDoc.InnerXml);
                UsuariosList usuarios = serial.DesSerializeObjeto<UsuariosList>(xmlDoc.InnerXml); 

                if (usuarios.Usuarios != null)
                {
                    if (usuarios.Usuarios.Where(u => u.Rut == usuario.Rut).Count() > 0)
                    {
                        var val = usuarios.Usuarios.Where(u => u.Rut == usuario.Rut).FirstOrDefault();
                        val.Nombre = usuario.Nombre;
                        val.Huella = usuario.Huella;
                    }
                    else
                    {
                        List<Usuario> usList = usuarios.Usuarios.ToList();
                        usList.Add(usuario);
                        usuarios.Usuarios = usList.ToArray();
                    }
                }
                else
                {
                    List<Usuario> usList = new List<Usuario>();
                    usList.Add(usuario);
                    usuarios.Usuarios = usList.ToArray();
                }

                XmlizedString = serial.SerializeObjeto<UsuariosList>(usuarios); //Funciones.SerializeObject(usuarios);
                xmlDoc = new XmlDocument();
                xmlDoc.InnerText = XmlizedString;
                //xmlDoc.Save(path);

                if(File.Exists(path))
                    File.Delete(path);

                xmlDoc.Save(path);

                MessageBox.Show(this, "El usuario ha sido ingresado correctamente", "Validador Huella", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, string.Format("Error al Intentar guardar el usuario. Detalle:{0}", ex.Message),
                    "Validador Huella", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        #endregion

        #region Metodos Privados

        protected void MakeReport(string message)
        {
            this.Invoke(new Function(delegate()
            {
                this.toolStripStatusLectorHuella.Text = message; //MessageBox.Show(message);
            }));
        }

        protected void SetStatus(string status)
        {
            this.Invoke(new Function(delegate()
            {
                this.lblInstruccionLector.Text = status;
            }));
        }

        protected DPFP.FeatureSet ExtractFeatures(DPFP.Sample Sample, DPFP.Processing.DataPurpose Purpose)
        {
            DPFP.Processing.FeatureExtraction Extractor = new DPFP.Processing.FeatureExtraction();	// Create a feature extractor
            DPFP.Capture.CaptureFeedback feedback = DPFP.Capture.CaptureFeedback.None;
            DPFP.FeatureSet features = new DPFP.FeatureSet();
            Extractor.CreateFeatureSet(Sample, Purpose, ref feedback, ref features);			// TODO: return features as a result?
            if (feedback == DPFP.Capture.CaptureFeedback.Good)
                return features;
            else
                return null;
        }

        protected void Process(DPFP.Sample Sample)
        {

            MemSample = Sample;
            // Draw fingerprint sample image.
            DrawPicture(ConvertSampleToBitmap(Sample));

            DPFP.FeatureSet features = ExtractFeatures(Sample, DPFP.Processing.DataPurpose.Enrollment);
            // Check quality of the sample and add to enroller if it's good
            if (features != null)
            {
                try
                {
                    MakeReport("The fingerprint feature set was created.");
                    Enroller.AddFeatures(features);		// Add feature set to template.
                }
                finally
                {
                    //UpdateStatus();

                    // Check if template has been created.
                    switch (Enroller.TemplateStatus)
                    {
                        case DPFP.Processing.Enrollment.Status.Ready:	// report success and stop capturing
                            MemTemplate = Enroller.Template;
                            EnabledDisabledButton(true);
                            //OnTemplate(Enroller.Template);
                            //SetPrompt("Click Close, and then click Fingerprint Verification.");
                            //Stop();
                            break;

                        case DPFP.Processing.Enrollment.Status.Failed:	// report failure and restart capturing

                            SetStatus(string.Format("Faltan {0} Muestras!", Enroller.FeaturesNeeded));
                            Enroller.Clear();
                            MemTemplate = null;
                            EnabledDisabledButton(false);
                            //Stop();
                            //UpdateStatus();
                            //OnTemplate(null);
                            //Start();
                            break;
                        case DPFP.Processing.Enrollment.Status.Insufficient:
                            SetStatus(string.Format("Faltan {0} Muestras!", Enroller.FeaturesNeeded));
                            EnabledDisabledButton(false);
                            break;
                    }
                }
            }
        }

        private void DrawPicture(Bitmap bitmap)
        {
            this.Invoke(new Function(delegate()
            {
                picHuella.Image = new Bitmap(bitmap, picHuella.Size);	// fit the image into the picture box
            }));
        }

        private void EnabledDisabledButton(bool enabled)
        {
            this.Invoke(new Function(delegate()
            {
                this.btnIngresar.Enabled = enabled;
            }));
        }

        protected Bitmap ConvertSampleToBitmap(DPFP.Sample Sample)
        {
            DPFP.Capture.SampleConversion Convertor = new DPFP.Capture.SampleConversion();	// Create a sample convertor.
            Bitmap bitmap = null;												            // TODO: the size doesn't matter
            Convertor.ConvertToPicture(Sample, ref bitmap);									// TODO: return bitmap as a result
            return bitmap;
        }

        #endregion

        #region Eventos del Lector

        public void OnComplete(object Capture, string ReaderSerialNumber, DPFP.Sample Sample)
        {
            //MakeReport("La huella digital fue capturada.");
            //SetPrompt("Scan the same fingerprint again.");
            SetStatus("La huella digital fue capturada!");
            MakeReport("Se ha capturado la huella digital");
            Process(Sample);
        }

        public void OnFingerGone(object Capture, string ReaderSerialNumber)
        {
            MakeReport("Ha sacado el dedo del lector de huella digital.");
        }

        public void OnFingerTouch(object Capture, string ReaderSerialNumber)
        {
            MakeReport("El lector de huella ha sido tocado.");
        }

        public void OnReaderConnect(object Capture, string ReaderSerialNumber)
        {
            MakeReport("El lector de huella esta conectado.");
        }

        public void OnReaderDisconnect(object Capture, string ReaderSerialNumber)
        {
            MakeReport("El lector de huella ha sido desconectado.");
        }

        public void OnSampleQuality(object Capture, string ReaderSerialNumber, DPFP.Capture.CaptureFeedback CaptureFeedback)
        {
            if (CaptureFeedback == DPFP.Capture.CaptureFeedback.Good)
                MakeReport("La calidad de la muestra de huella digital ha sido buena.");
            else
                MakeReport("La calidad de la muestra de huella digital no ha sido del todo buena.");
        }

        #endregion
    }
}
