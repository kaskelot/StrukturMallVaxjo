using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using VMS.TPS.Common.Model.API;

namespace AnpassaStrukturset
{
    public partial class MainForm : Form
    {
        private StrukturManipulering sm;
        private Patient _pat;
        private StructureSet _strSet;
        private User _user;
        public MainForm(StructureSet strSet, Patient pat, User user)
        {
            InitializeComponent();
            this._pat = pat;
            this._strSet = strSet;
            this._user = user;
            sm = new StrukturManipulering(_pat);
            InitializeGUI();
        }

        private void InitializeGUI()
        {
            cB_strukturmall.Items.Clear();
            Dictionary<string, string> templates = sm.GetAllTemplateNames();
            List<KeyValuePair<string, string>> sorted = new List<KeyValuePair<string, string>>(templates);
            sorted.Sort((a, b) => string.Compare(a.Value, b.Value, StringComparison.OrdinalIgnoreCase));
            cB_strukturmall.DataSource = sorted;
            cB_strukturmall.DisplayMember = "Value";
            cB_strukturmall.ValueMember = "Key";
        }

        private void btn_Ok_Click(object sender, EventArgs e)
        {
            string fileName = ((KeyValuePair<string, string>)cB_strukturmall.SelectedItem).Key;
            StructureTemplates curr_structTemp = sm.ImportStructureTemplate(fileName);
            StructureSet dtDos = sm.FindARTPlanStructureSet(_pat, _strSet, ((KeyValuePair<string, string>)cB_strukturmall.SelectedItem).Value, _user);
            var (removedStructures, addedStructures) = sm.CopyStructuresToDTDos(dtDos, curr_structTemp);
            //sm.ColorCorrector(dtDos, curr_structTemp);

            string msg = "Klart!\n\n";

            // Added
            msg += "Tillagda strukturer:\n";
            if (addedStructures.Count == 0)
                msg += "  (Inga)\n";
            else
                msg += string.Join("\n", addedStructures.Select(s => "  • " + s)) + "\n";

            // Removed
            msg += "\nBorttagna strukturer:\n";
            if (removedStructures.Count == 0)
                msg += "  (Inga)\n";
            else
                msg += string.Join("\n", removedStructures.Select(s => "  • " + s)) + "\n";

            MessageBox.Show(msg, "Resultat");

            // stäng GUI
            this.Close();

            //Close();
        }
    }
}