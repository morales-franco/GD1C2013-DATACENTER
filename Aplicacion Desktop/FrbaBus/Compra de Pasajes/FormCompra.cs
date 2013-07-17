﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using FrbaBus.Login;

namespace FrbaBus.Compra_de_Pasajes
{
    public partial class FormCompra : Form
    {
        //Atributos

        public string cod_viaje_pasaje="";
        public string cod_viaje_encomienda = "";
        public char tipo_viaje; //indica si el viaje es por encomienda o pasaje 'E' 'P' solo para seleccionar viaje 
        decimal total_compra;
        string cod_compra;
        //Colección de Pasajes una vez confirmada la compra los cargamos en la base
        List<cargar_pasajero> listas_pasajeros = new List<cargar_pasajero>();

        //Colección de Encomiendas una vez confirmada la compra las cargamos en la base
        List<Form_encomienda> listas_encomiendas = new List<Form_encomienda>();

        //Datos del Comprador
        public string dni_comprador = "";
        public string tipo_tarjeta = "";

        public FormCompra()
        {
            InitializeComponent();
            this.fecha_tbox.Enabled = false;
        }

        private void login_boton_Click(object sender, EventArgs e)
        {
            //cuando un administrador hace click en login se le abre la
            //pantalla de login
            login login = new login();
            login.ShowDialog();
        }

        private void select_boton_Click(object sender, EventArgs e)
        {
            select_fecha_viaje select_viaje = new select_fecha_viaje(this);
            select_viaje.ShowDialog();
        }

        private void FormCompra_Load(object sender, EventArgs e)
        {
            string query = "SELECT ciu_nombre FROM DATACENTER.Ciudad";
            connection conexion = new connection();
            DataTable table_ciu_orig= conexion.execute_query(query);
            DataTable table_ciu_dest = conexion.execute_query(query);
            
            //Cargo ciudades de Origen
            this.ciu_orig_list.DataSource = table_ciu_orig;
            this.ciu_orig_list.DisplayMember = "ciu_nombre";
            this.ciu_orig_list.ValueMember = "ciu_nombre";

            //Cargo ciudades Destino
            this.ciu_dest_list.DataSource = table_ciu_dest;
            this.ciu_dest_list.DisplayMember = "ciu_nombre";
            this.ciu_dest_list.ValueMember = "ciu_nombre";
            this.total_tbox.Text = total_compra.ToString();

            this.cant_totKg_tbox.Text = "0";
            this.sub_tot_encom_tbox.Text = "0";
            this.sub_total_pasaj_tbox.Text = "0";
            this.total_tbox.Text="0";

        }

        private void busc_viaje_boton_Click(object sender, EventArgs e)
        {

            bool error = false;

            if (this.fecha_tbox.Text == "")
            {
                MessageBox.Show("Debe Seleccionar Fecha", "Compra", MessageBoxButtons.OK, MessageBoxIcon.Error);
                error = true;
            }

            if (error)
                return;
            this.ciu_orig_list.Enabled = false;
            this.ciu_dest_list.Enabled = false;

            this.tipo_viaje = 'P';
            select_viaje seleccionar_viaje = new select_viaje(this);
            seleccionar_viaje.ShowDialog();
        }

        private void cargar_pas_boton_Click(object sender, EventArgs e)
        {
            bool error = false;
            

            if (this.CantPasaj_numericUpDown.Value <= 0)
            {
                MessageBox.Show("Cantidad de Pasajes Incorrecta", "Compra", MessageBoxButtons.OK, MessageBoxIcon.Error);
                error = true;
            }

            if (this.cod_viaje_pasaje == "")
            {
                MessageBox.Show("Debe Seleccionar Viaje para el Pasaje", "Compra", MessageBoxButtons.OK, MessageBoxIcon.Error);
                error = true;
            }

            if (error)
                return;

            this.CantPasaj_numericUpDown.Enabled = false;
            int cant_pasajes = Convert.ToInt16(this.CantPasaj_numericUpDown.Value);
            int i;

            //cargamos los datos de los pasajeros
            this.tipo_viaje='P'; //necesario porq encomienda y pasaje tienen mismo recorrido pero pueden realizarlo en distintos viajes
            select_viaje form_viaje = new select_viaje(this); //con esto me aseguro que siempre sea el mismo recorrido
            
            //int contador_discapacitados= 0;
            bool sgte_acompañante = false;
            string sexo;
            funciones func = new funciones();
            for (i = 0; i < cant_pasajes; i++)
            {
                cargar_pasajero pasajero = new cargar_pasajero(this.cod_viaje_pasaje, listas_pasajeros, sgte_acompañante );
                pasajero.ShowDialog();
                if (pasajero.discapacitado_checkB.Checked)
                {
                    if (i != cant_pasajes - 1)//verificamos que ni sea el ultimo pasajero
                    {
                        DialogResult respuesta = MessageBox.Show("Datos Ingresados Correctamente. El pasajero Ingresado es discapacitado, ¿viaja con acompañante ?", "Compra", MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation);
                        if (respuesta == DialogResult.Yes)
                        {
                            sgte_acompañante = true;
                            MessageBox.Show("Ingrese los datos del acompañante del pasajero Discapacitado", "Compra", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                    }
                }
                else
                {
                    sgte_acompañante = false;
                    /*----------Si no es discapacitado analizo si es Pensionado / Jubilado----------------*/
                    
                    if (pasajero.mascul_radioBut.Checked)
                        sexo = "M";
                    else
                        sexo = "F";

                    if (pasajero.pensionado_checkB.Checked | func.es_jubilado(pasajero.fec_nac_Tbox.Text,sexo))
                        pasajero.costo_pasaje = pasajero.costo_pasaje / 2; //aplico descuento del 50%

                    if (cant_pasajes - 1 != i)
                    {
                        MessageBox.Show("Datos del Pasajero Ingresados Correctamente. A continuación debe seleccionar viaje del siguiente Pasajero", "Compra", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        form_viaje.ShowDialog();
                    }
                }
                    
                listas_pasajeros.Add(pasajero);

            }

            /*---------Una vez cargados todos los pasajes calculo el costo total-------*/
            stored_procedures stored_proc = new stored_procedures();
            decimal sub_total_compra_pasaj = 0;
            foreach (cargar_pasajero pasaje in listas_pasajeros)
            {
                sub_total_compra_pasaj += pasaje.costo_pasaje;
            }
            this.sub_total_pasaj_tbox.Text = sub_total_compra_pasaj.ToString("N2"); //muestre 2 decimas
            this.total_compra += sub_total_compra_pasaj;
            this.total_tbox.Text = this.total_compra.ToString("N2");
            this.cargar_pas_boton.Enabled = false;
        }

        private void NroPasaj_numericUpDown_KeyPress(object sender, KeyPressEventArgs e)
        {
            //solo permite q ingrese numeros
            if (char.IsNumber(e.KeyChar) | char.IsControl(e.KeyChar) )
                e.Handled = false;
            else
                e.Handled = true;
        }



        private void selec_viaje_encom_button_Click(object sender, EventArgs e)
        {
            
            bool error = false;


            if (this.fecha_tbox.Text == "")
            {
                MessageBox.Show("Debe Seleccionar Fecha", "Compra", MessageBoxButtons.OK, MessageBoxIcon.Error);
                error = true;
            }

            if (error)
                return;
            this.ciu_orig_list.Enabled = false;
            this.ciu_dest_list.Enabled = false;

            this.tipo_viaje = 'E';
            select_viaje seleccionar_viaje = new select_viaje(this);
            seleccionar_viaje.ShowDialog();
        }

        private void carg_encom_boton_Click(object sender, EventArgs e)
        {
            bool error = false;

            if (this.cant_encomiendas_numUpdown.Value <= 0)
            {
                MessageBox.Show("Cantidad de Encomiendas Incorrecta", "Compra", MessageBoxButtons.OK, MessageBoxIcon.Error);
                error = true;
            }

            if (this.cod_viaje_encomienda == "")
            {
                MessageBox.Show("Debe Seleccionar Viaje para la Encomienda", "Compra", MessageBoxButtons.OK, MessageBoxIcon.Error);
                error = true;
            }

            if (error)
                return;

            this.cant_encomiendas_numUpdown.Enabled = false;
            int cant_encomiendas = Convert.ToInt16(this.cant_encomiendas_numUpdown.Value);
            int i;

            //cargamos datos de la encomienda
            this.tipo_viaje='E'; //le decimos que los viajes a seleccionar seran para envio de encomiendas
            select_viaje form_viaje = new select_viaje(this); //con esto aseguro que sea siempre el mismo recorrido NO dejo q me setee mas los campos de ciu_origen y dest
            for (i = 0; i < cant_encomiendas; i++)
            {
                Form_encomienda form_encom = new Form_encomienda(this.cod_viaje_encomienda, this.listas_encomiendas);
                form_encom.ShowDialog();
                listas_encomiendas.Add(form_encom);
                if (cant_encomiendas - 1 != i)
                {
                    MessageBox.Show("Datos de la Encomienda Ingresados, A continuación debe seleccionar viaje de la siguiente Encomienda", "Comprar", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    form_viaje.ShowDialog();
                }
                

            }
            stored_procedures stored_proc = new stored_procedures();
            decimal sub_total_compra_encomienda = 0;
            int total_KG_encomienda = 0; //solo se ingresan valores enteros de peso
            foreach (Form_encomienda encomienda in listas_encomiendas)
            {
                sub_total_compra_encomienda += Convert.ToDecimal(stored_proc.get_costo_encomienda(encomienda.viaje_cod, encomienda.peso_encom_tbox.Text));
                total_KG_encomienda += Convert.ToInt16(encomienda.peso_encom_tbox.Text);
            }
            this.sub_tot_encom_tbox.Text= sub_total_compra_encomienda.ToString("N2");
            this.cant_totKg_tbox.Text = total_KG_encomienda.ToString();
            this.total_compra += sub_total_compra_encomienda;
            this.total_tbox.Text = this.total_compra.ToString("N2");
            this.carg_encom_boton.Enabled = false;
        }

        private void reset_formulario()
        {
            this.fecha_tbox.Clear();
            this.fecha_tbox.Enabled = true;
            this.ciu_orig_list.SelectedIndex = 0;
            this.ciu_dest_list.SelectedIndex = 0;
            this.ciu_orig_list.Enabled = true;
            this.ciu_dest_list.Enabled = true;
            this.CantPasaj_numericUpDown.Value = this.CantPasaj_numericUpDown.Minimum;
            this.sub_total_pasaj_tbox.Clear();
            this.cant_encomiendas_numUpdown.Value = this.cant_encomiendas_numUpdown.Minimum;
            this.cant_totKg_tbox.Clear();
            this.sub_tot_encom_tbox.Clear();
            this.total_tbox.Clear();
            this.cant_encomiendas_numUpdown.Enabled = true;
            this.CantPasaj_numericUpDown.Enabled = true;
            this.cargar_pas_boton.Enabled = true;
            this.carg_encom_boton.Enabled = true;
            this.cant_totKg_tbox.Text = "0";
            this.sub_tot_encom_tbox.Text = "0";
            this.sub_total_pasaj_tbox.Text = "0";
            this.total_tbox.Text = "0";
            this.total_compra = 0;

            this.cod_viaje_encomienda = "";
            this.cod_viaje_pasaje = "";
            this.listas_encomiendas.Clear();
            this.listas_pasajeros.Clear();
        }

        private void cancelar_boton_Click(object sender, EventArgs e)
        {
            this.reset_formulario();    

        }

        private void aceptar_boton_Click(object sender, EventArgs e)
        {

            if (this.cant_encomiendas_numUpdown.Value == 0 & this.CantPasaj_numericUpDown.Value == 0)
            {
                MessageBox.Show("No se han Ingresado Datos Obligatorios para hacer la compra", "Compra", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            this.total_tbox.Text = total_compra.ToString("N2");
            Form_Comprador comprador = new Form_Comprador(this);
            comprador.ShowDialog();

            
            /*--------------Insertamos y Mostramos Compra/Pasaje/Encomienda------------------*/
            stored_procedures stored_proc = new stored_procedures();
            
            this.cod_compra = stored_proc.insert_compra(this.dni_comprador, this.tipo_tarjeta, this.CantPasaj_numericUpDown.Value.ToString(), this.cant_totKg_tbox.Text, this.total_compra);
            MessageBox.Show("Compra registrada Correctamente. Codigo de Compra: "+this.cod_compra+" DNI del comprador:"+this.dni_comprador);

            if (listas_pasajeros.Count > 0)
            {
                string cod_pasaje = "";
                foreach (cargar_pasajero pasajero in listas_pasajeros)
                {
                    cod_pasaje = stored_proc.insert_pasaje(pasajero.butNro_tbox.Text, stored_proc.get_micro_patente(pasajero.viaje_cod), pasajero.DNI_Tbox.Text, this.cod_compra, pasajero.costo_pasaje, pasajero.viaje_cod);
                    MessageBox.Show("Codigo de Pasaje: " + cod_pasaje + " Nro DNI pasajero: " + pasajero.DNI_Tbox.Text + "Nombre: " + pasajero.nombre_Tbox.Text + "Apellido: " + pasajero.apell_Tbox.Text + " Butaca Nro: " + pasajero.butNro_tbox.Text + "Piso: " + pasajero.piso_tbox.Text + "Tipo: " + pasajero.pos_but_tbox.Text);
                }
            }

            if (listas_pasajeros.Count > 0)
            {
                string cod_encomienda = "";
                foreach (Form_encomienda encomienda in listas_encomiendas)
                {
                    cod_encomienda = stored_proc.insert_paquete(this.cod_compra, Convert.ToDecimal(encomienda.precio_encomiendaTbox.Text), encomienda.peso_encom_tbox.Text, encomienda.viaje_cod);
                    MessageBox.Show("Codigo del Paquete: " + cod_encomienda + " Nro DNI del Remitente: " + encomienda.DNI_Tbox.Text + "Nombre: " + encomienda.nombre_Tbox.Text + "Apellido: " + encomienda.apell_Tbox.Text + "Peso del Paquete: "+ encomienda.peso_encom_tbox.Text+" kg");
                }
            }
            

            this.reset_formulario();

        }

        private void cant_encomiendas_numUpdown_KeyPress(object sender, KeyPressEventArgs e)
        {
            //solo permite q ingrese numeros
            if (char.IsNumber(e.KeyChar) | char.IsControl(e.KeyChar))
                e.Handled = false;
            else
                e.Handled = true;
        }
  
    }
}
