using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using System.IO;
using ValidadorHuella.DTO;
using ValidadorHuella.Utils;
using System.Linq;
using DPFP;

namespace ValidadorHuella
{
    public partial class ValidarUsuario : Form, DPFP.Capture.EventHandler
    {
        #region Propiedades Privadas
        delegate void Function();
        private DPFP.Capture.Capture Capturer;
        private DPFP.Processing.Enrollment Enroller;
        private DPFP.Template Template;
        private DPFP.Verification.Verification Verificator;
        private DPFP.Sample MemSample;
        #endregion

        #region Constructores
        public ValidarUsuario()
        {
            InitializeComponent();
            Verificator = new DPFP.Verification.Verification();
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

        protected void Process(DPFP.Sample Sample)
        {
            try
            {
                MemSample = Sample;
                // Draw fingerprint sample image.
                DrawPicture(ConvertSampleToBitmap(Sample));
                EnabledDisabledButton(true);
            }
            catch (Exception ex)
            {
                MakeReport(ex.Message);
            }
        }

        protected void Compare(Template template, Sample sample)
        {
            try
            {
                // Process the sample and create a feature set for the enrollment purpose.
                //MemoryStream ms = new MemoryStream(Funciones.ImageToByteArray(this.picHuella.Image));
                //DPFP.Sample sample = new DPFP.Sample(ms);
                DPFP.FeatureSet features = ExtractFeatures(sample, DPFP.Processing.DataPurpose.Verification);

                // Check quality of the sample and start verification if it's good
                // TODO: move to a separate task
                if (features != null)
                {
                    // Compare the feature set with our template
                    DPFP.Verification.Verification.Result result = new DPFP.Verification.Verification.Result();
                    Verificator.Verify(features, template, ref result);
                    //UpdateStatus(result.FARAchieved);
                    if (result.Verified)
                    {
                        MessageBox.Show(this, "El usuario coincide con el existente!", "Validador Huella", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        MakeReport("El usuario coincide con el existente!");
                    }
                    else
                    {
                        MessageBox.Show(this, "El usuario No coincide con el existente!", "Validador Huella", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        MakeReport("El usuario No coincide con el existente!.");
                    }
                }
            }
            catch (Exception ex)
            {
                MakeReport(ex.Message);
            }
        }

        private void EnabledDisabledButton(bool enabled)
        {
            this.Invoke(new Function(delegate()
            {
                this.btnValidar.Enabled = enabled;
            }));
        }

        private void DrawPicture(Bitmap bitmap)
        {
            this.Invoke(new Function(delegate()
            {
                picHuella.Image = new Bitmap(bitmap, picHuella.Size);	// fit the image into the picture box
            }));
        }

        protected Bitmap ConvertSampleToBitmap(DPFP.Sample Sample)
        {
            DPFP.Capture.SampleConversion Convertor = new DPFP.Capture.SampleConversion();	// Create a sample convertor.
            Bitmap bitmap = null;												            // TODO: the size doesn't matter
            Convertor.ConvertToPicture(Sample, ref bitmap);									// TODO: return bitmap as a result
            return bitmap;
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

        #region Eventos del Formulario
        private void btnLeerHuella_Click(object sender, EventArgs e)
        {
            try
            {
                Capturer = new DPFP.Capture.Capture();				// Create a capture operation.

                if (null != Capturer)
                {
                    Capturer.EventHandler = this;
                    Enroller = new DPFP.Processing.Enrollment();
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

        private void btnValidar_Click(object sender, EventArgs e)
        {
            try
            {
                XmlSerial serial = new XmlSerial();
                XmlDocument xmlDoc = new XmlDocument();
                string path = Path.GetFullPath("DB/UsuarioRepository.xml");

                xmlDoc.Load(path);
                //(UsuariosList)Funciones.DeserializeObject<UsuariosList>(xmlDoc.InnerXml);
                UsuariosList usuarios = serial.DesSerializeObjeto<UsuariosList>(xmlDoc.InnerXml);
                var usuario = usuarios.Usuarios.Where(u => u.Rut == this.txtRut.Text).FirstOrDefault();
                MemoryStream ms = new MemoryStream(usuario.TemplateBytes);
                //DPFP.Capture.SampleConversion convertor = new DPFP.Capture.SampleConversion();
                //convertor.ConvertToANSI381(usuarios.Usuarios.Where(u => u.Rut == this.txtRut.Text).FirstOrDefault().Sample, ref bytes);
                //new DPFP.Template(new MemoryStream(bytes));
                Template template = new DPFP.Template(ms);
                Compare(template, MemSample);
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
        #endregion

    }
}
