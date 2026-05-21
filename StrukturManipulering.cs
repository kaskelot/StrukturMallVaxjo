using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using VMS.TPS.Common.Model.API;
using System.Windows.Media;
using System.Xml.Linq;

namespace StrukturMall
{
    internal class StrukturManipulering
    {
        //private string folderPath = @"\\S1574\va_data$\ProgramData\Vision\Templates\structure\"; // tbox
        private string folderPath = @"\\ariafile\Aria_Files\data\ProgramData\Vision\Templates\structure\"; // klinisk

        public StrukturManipulering(Patient pat)
        {
            pat.BeginModifications();
        }



        public Dictionary<string, string> GetAllTemplateNames()
        {

            if (!Directory.Exists(folderPath))
            {
                throw new ApplicationException("Kunde inte hitta templatemapp: " + folderPath);
            }

            string[] xmlFiles = Directory.GetFiles(folderPath, "StructureTemplate_*.xml");
            Dictionary<string, string> templates = new Dictionary<string, string>();
            foreach (string xmlFile in xmlFiles)
            {
                try
                {
                    XDocument doc = XDocument.Load(xmlFile);
                    XElement preview = doc.Root.Element("Preview");
                    if (preview == null) continue;

                    XAttribute approvalStatus = preview.Attribute("ApprovalStatus");
                    if (approvalStatus == null || approvalStatus.Value != "Approved") continue;

                    XAttribute idAttr = preview.Attribute("ID");
                    if (idAttr == null) continue;

                    string fileName = Path.GetFileName(xmlFile).Replace(".xml", "");
                    templates.Add(fileName, idAttr.Value);
                }
                catch
                {
                    continue;
                }
            }
            return templates;
        }

        public StructureTemplates ImportStructureTemplate(string fileName)
        {
            StructureTemplates curr_structureTemplate;
            // Get all XML files in the folder
            string xmlFilePath = folderPath + fileName + ".xml";

            XmlRootAttribute xRoot = new XmlRootAttribute
            {
                ElementName = "StructureTemplate",
                Namespace = ""  // Matches the empty namespace in your XML
            };

            XmlSerializer serializer = new XmlSerializer(typeof(StructureTemplates), xRoot);

            // Deserialize the XML file into a StructureTemplates object
            using (StreamReader reader = new StreamReader(xmlFilePath))
            {
                curr_structureTemplate = (StructureTemplates)serializer.Deserialize(reader);
            }
            return curr_structureTemplate;
        }

        public StructureSet FindARTPlanStructureSet(Patient pat, StructureSet strSet, string valtTemplate, User user)
        {
            StructureSet dtDos = strSet.Copy();
            string datum = dtDos.Image.CreationDateTime.Value.ToString("yyMMdd");

            string nyttId = datum;
            int i = 1;

            while (pat.Studies.SelectMany(x => x.Series)
                              .SelectMany(x => x.Images)
                              .Any(x => x.Id == nyttId))
            {
                nyttId = datum + "_" + i;
                i++;
            }

            dtDos.Image.Id = nyttId;

            string ssId = valtTemplate.Length > 16 ? valtTemplate.Substring(0, 16) : valtTemplate;
            ssId = FindSuitableID(ssId, pat);
            dtDos.Id = ssId;

            string name = valtTemplate.Length > 64 ? valtTemplate.Substring(0, 64) : valtTemplate;
            dtDos.Name = name;

            return dtDos;
        }

        public string FindSuitableID(string dtID, Patient pat)
        {
            bool checkId = pat.StructureSets.Any(ss => ss.Id.ToLower().Equals(dtID.ToLower()));
            if (checkId)
            {
                int i = 1;
                string temp_id = "";
                while (checkId)
                {
                    temp_id = dtID + "_" + i;
                    i++;
                    checkId = pat.StructureSets.Any(ss => ss.Id.ToLower().Equals(temp_id.ToLower()));
                }
                dtID = temp_id;
            }
            return dtID;
        }

        //// skapar union av rectum och anorectum
        //public void MergeRectumStructures(StructureSet strSet)
        //{
        //    Structure temp_Rectum = strSet.Structures.FirstOrDefault(sss => sss.Id.Equals("Rectum"));
        //    Structure temp_Anorectum = strSet.Structures.FirstOrDefault(sss => sss.Id.Equals("Anorectum"));
        //    if (temp_Rectum != null && temp_Anorectum != null)
        //    {
        //        if (temp_Anorectum.CanConvertToHighResolution())
        //            temp_Anorectum.ConvertToHighResolution();
        //        if (temp_Rectum.CanConvertToHighResolution())
        //            temp_Rectum.ConvertToHighResolution();
        //        strSet.Structures.FirstOrDefault(sss => sss.Id.Equals("Rectum")).SegmentVolume = temp_Rectum.SegmentVolume.Or(temp_Anorectum.SegmentVolume);
        //    }
        //}
        //// skapar union av Glottis och LarynxSG
        //public void MergeLarynxStructures(StructureSet strSet)
        //{
        //    Structure temp_Glottis = strSet.Structures.FirstOrDefault(sss => sss.Id.Equals("Glottis"));
        //    Structure temp_LarynxSG = strSet.Structures.FirstOrDefault(sss => sss.Id.Equals("LarynxSG"));
        //    Structure temp_Larynx = strSet.Structures.FirstOrDefault(sss => sss.Id.Equals("Larynx"));

        //    if (temp_Glottis != null && temp_LarynxSG != null && temp_Larynx == null)
        //    {
        //        if (temp_Glottis.CanConvertToHighResolution())
        //            temp_Glottis.ConvertToHighResolution();
        //        if (temp_LarynxSG.CanConvertToHighResolution())
        //            temp_LarynxSG.ConvertToHighResolution();
        //        Structure larynx = strSet.AddStructure("Organ", "Larynx");
        //        if (larynx.CanConvertToHighResolution())
        //            larynx.ConvertToHighResolution();
        //        larynx.SegmentVolume = temp_Glottis.SegmentVolume.Or(temp_LarynxSG.SegmentVolume);
        //    }
        //}

        public (List<string> removed, List<string> added) CopyStructuresToDTDos(StructureSet dtDos, StructureTemplates curr_structureTemplate)
        {
            // Hittar alla strukturer i ART-plan som inte finns i valt template - lägger i lista
            List<Structure> listOfStructuresToRemove = new List<Structure>();
            List<string> removed = new List<string>();
            List<string> added = new List<string>();
            foreach (Structure temp_structure in dtDos.Structures)
            {
                var removeStructure = curr_structureTemplate.Structures.FirstOrDefault(s => s.ID.Equals(temp_structure.Id));
                if (removeStructure == null && temp_structure.DicomType != "")
                {
                    listOfStructuresToRemove.Add(temp_structure);
                }
            }
            // Tar bort alla strukturer som finns i listan
            foreach (Structure temp_structure in listOfStructuresToRemove)
            {
                Console.WriteLine("Tar bort: " + temp_structure.Id);
                bool ok = temp_structure.CanEditSegmentVolume(out string errorMsg);
                if (ok)
                {
                    removed.Add(temp_structure.Id);
                    dtDos.RemoveStructure(temp_structure);
                }
                else
                {
                    Console.WriteLine(errorMsg);
                }
            }

            // Lägg till tomma strukturer från template till strukturset (dvs PTV, CTV, PRV_xxx)
            List<StructureTemplateStructuresStructure> listOfStructuresToAdd = new List<StructureTemplateStructuresStructure>();

            foreach (StructureTemplateStructuresStructure templateStruktur in curr_structureTemplate.Structures)
            {
                var addStructure = dtDos.Structures.FirstOrDefault(s => s.Id.Equals(templateStruktur.ID));
                if (addStructure == null)
                {
                    listOfStructuresToAdd.Add(templateStruktur);
                }
            }
            foreach (StructureTemplateStructuresStructure templateStruktur in listOfStructuresToAdd)
            {
                Console.WriteLine("Lägger till: " + templateStruktur.ID);
                bool structOk = dtDos.CanAddStructure(templateStruktur.Identification.First().VolumeType, templateStruktur.ID);
                if (structOk)
                {
                    dtDos.AddStructure(templateStruktur.Identification.First().VolumeType, templateStruktur.ID);
                    added.Add(templateStruktur.ID);
                }
                else
                {
                    dtDos.AddStructure("Control", templateStruktur.ID);
                }
            }
            return (removed, added);
        }

        //public void ColorCorrector(StructureSet dtDos, StructureTemplates curr_structureTemplate)
        //{
        //    // Fixar färgerna till samma färger som i aktuell template
        //    foreach (Structure s in dtDos.Structures)
        //    {
        //        StructureTemplateStructuresStructure templateStruktur = curr_structureTemplate.Structures.FirstOrDefault(sss => sss.ID.Equals(s.Id));
        //        if (templateStruktur != null)
        //        {
        //            //get color from template
        //            string input = templateStruktur.ColorAndStyle;

        //            // Split by space
        //            string[] parts = input.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        //            try
        //            {
        //                // Parse the last three parts as integers
        //                byte r = byte.Parse(parts[1]);
        //                byte g = byte.Parse(parts[2]);
        //                byte b = byte.Parse(parts[3]);
        //                //alfa-värde (opacity)
        //                byte a = AlphaValue(s);

        //                Color newcol = Color.FromArgb(a, r, g, b);
        //                s.Color = newcol;
        //            }
        //                catch (Exception e)
        //            {
        //                Console.Error.WriteLine(e.ToString());
        //            }
        //        }
        //    }
        //}
        //private byte AlphaValue(Structure s)
        //{
        //    byte alfa = 128;
        //    if (s.DicomType.Equals("PTV") || s.DicomType.Equals("CTV"))
        //    {
        //        alfa = 200;
        //    }
        //    //else if ()
        //    //{
        //    //    alfa = 200;
        //    //}
        //    return alfa;
        //}
    }
}

