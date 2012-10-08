using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace ValidadorHuella
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
        }

        private void ingresarUsuarioToolStripMenuItem_Click(object sender, EventArgs e)
        {
            IngresarUsuario dialog = new IngresarUsuario();
            dialog.ShowDialog();
        }

        private void validarUsuarioToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ValidarUsuario dialog = new ValidarUsuario();
            dialog.ShowDialog();
        }

        private void salirToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (DialogResult.Yes == MessageBox.Show(this, "¿Desea Salir del Sistema?", "Validador Huella", MessageBoxButtons.YesNo, MessageBoxIcon.Question))
            {
                this.Close();
            }
        }
    }
}
