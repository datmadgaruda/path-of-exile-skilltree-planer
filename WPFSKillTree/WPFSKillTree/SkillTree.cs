﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Raven.Json.Linq;
using System.Diagnostics;

namespace POESKillTree
{
    public class SkillTree
    {

        public SkillTree(String treestring)
        {

            RavenJObject jObject = RavenJObject.Parse(treestring.Replace("Additional ", ""));

            int qindex = 0;



            foreach (RavenJObject jobj in ((RavenJArray)((RavenJObject)jObject["skillSprites"])["active"]))
            {
                iconActiveSkills.SkillPositions[qindex] = new Dictionary<string, Rect>();
                iconActiveSkills.Filename[qindex] = jobj["filename"].Value<string>();
                foreach (string s in ((RavenJObject)(jobj["coords"])).Keys)
                {
                    RavenJObject o = (RavenJObject)(((RavenJObject)jobj["coords"])[s]);
                    iconActiveSkills.SkillPositions[qindex][s] = new Rect(o["x"].Value<int>(), o["y"].Value<int>(),
                                                                          o["w"].Value<int>(), o["h"].Value<int>());
                }
                qindex++;
            }
            qindex = 0;
            foreach (RavenJObject jobj in ((RavenJArray)((RavenJObject)jObject["skillSprites"])["inactive"]))
            {
                iconInActiveSkills.SkillPositions[qindex] = new Dictionary<string, Rect>();
                iconInActiveSkills.Filename[qindex] = jobj["filename"].Value<string>();
                foreach (string s in ((RavenJObject)(jobj["coords"])).Keys)
                {
                    RavenJObject o = (RavenJObject)(((RavenJObject)jobj["coords"])[s]);
                    iconInActiveSkills.SkillPositions[qindex][s] = new Rect(o["x"].Value<int>(), o["y"].Value<int>(),
                                                                            o["w"].Value<int>(), o["h"].Value<int>());
                }
                qindex++;
            }

            foreach (string s in ((RavenJObject)(jObject["assets"])).Keys)
            {
                if (((RavenJObject)((RavenJObject)(jObject["assets"]))[s])["0.4794"].Value<string>() == null)
                    continue;
                assets[s] = new Asset(s,
                                      ((RavenJObject)((RavenJObject)(jObject["assets"]))[s])["0.4794"].Value
                                          <string>());
            }
            iconActiveSkills.OpenOrDownloadImages();
            iconInActiveSkills.OpenOrDownloadImages();

            foreach (string s in ((RavenJObject)jObject["characterData"]).Keys)
            {
                var co = ((RavenJObject)((RavenJObject)jObject["characterData"])[s]);
                CharBaseAttributes[int.Parse(s) - 1] = new Dictionary<string, float>() { { "+# to Strength", co["base_str"].Value<int>() }, { "+# to Dexterity", co["base_dex"].Value<int>() }, { "+# to Intelligence", co["base_int"].Value<int>() } };
            }
            foreach (RavenJObject token in jObject["nodes"].Values())
            {
                Skillnodes.Add(token["id"].Value<int>(), new SkillTree.SkillNode()
                                                             {
                                                                 id = token["id"].Value<int>(),
                                                                 a = token["a"].Value<int>(),
                                                                 name = token["dn"].Value<string>(),
                                                                 attributes = token["sd"].Values<string>().ToArray(),
                                                                 orbit = token["o"].Value<int>(),
                                                                 orbitIndex = token["oidx"].Value<int>(),
                                                                 icon = token["icon"].Value<string>(),
                                                                 linkID = token["out"].Values<int>().ToList(),
                                                                 g = token["g"].Value<int>(),
                                                                 da = token["da"].Value<int>(),
                                                                 ia = token["ia"].Value<int>(),
                                                                 ks = token["ks"].Value<bool>(),
                                                                 not = token["not"].Value<bool>(),
                                                                 sa = token["sa"].Value<int>(),



                                                             });
            }
            List<int[]> links = new List<int[]>();
            foreach (var skillNode in Skillnodes)
            {
                foreach (int i in skillNode.Value.linkID)
                {
                    if (
                        links.Count(
                            nd => (nd[0] == i && nd[1] == skillNode.Key) || nd[0] == skillNode.Key && nd[1] == i) ==
                        1)
                    {
                        continue;
                    }
                    links.Add(new int[] { skillNode.Key, i });
                }
            }
            foreach (int[] ints in links)
            {
                if (!Skillnodes[ints[0]].Neighbor.Contains(Skillnodes[ints[1]]))
                    Skillnodes[ints[0]].Neighbor.Add(Skillnodes[ints[1]]);
                if (!Skillnodes[ints[1]].Neighbor.Contains(Skillnodes[ints[0]]))
                    Skillnodes[ints[1]].Neighbor.Add(Skillnodes[ints[0]]);
            }
            // skillNode.Value.Neighbor.Add(Skillnodes[i]);
            foreach (RavenJObject token in jObject["groups"].Values())
            {
                NodeGroup ng = new NodeGroup();

                ng.Nodes = token["n"].Values<int>().ToList();
                if (!(token["oo"] is RavenJArray))
                    ng.OcpOrb = ((RavenJObject)token["oo"]).ToDictionary(k => k.Key,
                                                                          k =>
                                                                          k.Value.Value<bool>());
                if ((token["oo"] is RavenJArray))
                {
                    ng.OcpOrb.Add("0", true);
                }
                ng.Position = new Vector2D(token["x"].Value<float>(), token["y"].Value<float>());


                NodeGroups.Add(ng);
            }
            foreach (SkillTree.NodeGroup @group in NodeGroups)
            {
                foreach (int node in group.Nodes)
                {
                    Skillnodes[node].NodeGroup = group;
                }
            }

            TRect = new Rect2D(new Vector2D(jObject["min_x"].Value<float>() * 1.1, jObject["min_y"].Value<float>() * 1.1),
                               new Vector2D(jObject["max_x"].Value<float>() * 1.1, jObject["max_y"].Value<float>() * 1.1));
            //picSkillTree = new BitmapImage((int)imagesize, (int)imagesize);

            //   Graphics g = Graphics.FromImage(picSkillTree);
            //  g.SmoothingMode = SmoothingMode.AntiAlias;
            //  g.Clear(Color.Black);


            Pen pen = new Pen(Brushes.Black, 5);

            Pen pen3 = new Pen(Brushes.Green, 10);
            DrawingVisual drawingVisual = new DrawingVisual();

            Geometry g = new RectangleGeometry(TRect);

            Size sizeNot;
            ImageBrush brNot = new ImageBrush();
            brNot.Stretch = Stretch.Uniform;
            BitmapImage PImageNot = assets[nodeBackgrounds["notable"]].PImage;
            brNot.ImageSource = PImageNot;
            sizeNot = new Size(PImageNot.PixelWidth, PImageNot.PixelHeight);


            Size sizeKs;
            ImageBrush brKS = new ImageBrush();
            brKS.Stretch = Stretch.Uniform;
            BitmapImage PImageKr = assets[nodeBackgrounds["keystone"]].PImage;
            brKS.ImageSource = PImageKr;
            sizeKs = new Size(PImageKr.PixelWidth, PImageKr.PixelHeight);

            Size isizeNorm;
            ImageBrush brNorm = new ImageBrush();
            brNorm.Stretch = Stretch.Uniform;
            BitmapImage PImageNorm = assets[nodeBackgrounds["normal"]].PImage;
            brNorm.ImageSource = PImageNorm;
            isizeNorm = new Size(PImageNorm.PixelWidth, PImageNorm.PixelHeight);

            Size isizeNormA;
            ImageBrush brNormA = new ImageBrush();
            brNormA.Stretch = Stretch.Uniform;
            BitmapImage PImageNormA = assets[nodeBackgrounds["normalA"]].PImage;
            brNormA.ImageSource = PImageNormA;
            isizeNormA = new Size(PImageNormA.PixelWidth, PImageNormA.PixelHeight);

            NodeSurroundBrush.Add(new KeyValuePair<Size, ImageBrush>(isizeNorm, brNorm));
            NodeSurroundBrush.Add(new KeyValuePair<Size, ImageBrush>(isizeNormA, brNormA));
            NodeSurroundBrush.Add(new KeyValuePair<Size, ImageBrush>(sizeKs, brKS));
            NodeSurroundBrush.Add(new KeyValuePair<Size, ImageBrush>(sizeNot, brNot));

            using (DrawingContext dc = drawingVisual.RenderOpen())
            {
                dc.DrawGeometry(null, pen, g);
                foreach (var skillNode in Skillnodes)
                {
                    Vector2D pos = (skillNode.Value.Position);
                    skillNode.Value.normPos = pos;


                    // dc.DrawEllipse(Brushes.Transparent, pen, new Point(pos.X, pos.Y), 60, 60);
                    //  r.FillEllipse(Brushes.Black, (float)pos.X - 15, (float)pos.Y - 15, 30, 30);

                    if (skillNode.Value.linkID != null)
                        foreach (var i in skillNode.Value.linkID)
                        {
                            double arcstart = (float)skillNode.Value.Arc;
                            double arcrad = Skillnodes[i].Arc;
                            Vector2D linkpos = (Skillnodes[i].Position);

                            //  dc.DrawLine(pen2, new Point(linkpos.X, linkpos.Y), new Point(pos.X, pos.Y));
                            // g.DrawLine(new Pen(Color.Blue, 5), (float)linkpos.X, (float)linkpos.Y, (float)pos.X, ;
                        }

                }

                foreach (var skillNode in Skillnodes)
                {
                    Vector2D pos = (skillNode.Value.Position);
                    if (skillNode.Value.not)
                    {
                        dc.DrawRectangle(NodeSurroundBrush[3].Value, null,
                                         new Rect((int)pos.X - NodeSurroundBrush[3].Key.Width, (int)pos.Y - NodeSurroundBrush[3].Key.Height, NodeSurroundBrush[3].Key.Width * 2,
                                                  NodeSurroundBrush[3].Key.Height * 2));
                    }
                    else if (skillNode.Value.ks)
                    {
                        dc.DrawRectangle(NodeSurroundBrush[2].Value, null,
                                         new Rect((int)pos.X - NodeSurroundBrush[2].Key.Width, (int)pos.Y - NodeSurroundBrush[2].Key.Height, NodeSurroundBrush[2].Key.Width * 2,
                                                  NodeSurroundBrush[2].Key.Height * 2));
                    }
                   
                        dc.DrawRectangle(NodeSurroundBrush[0].Value, null,
                                         new Rect((int)pos.X - NodeSurroundBrush[0].Key.Width, (int)pos.Y - NodeSurroundBrush[0].Key.Height,
                                                  NodeSurroundBrush[0].Key.Width * 2, NodeSurroundBrush[0].Key.Height * 2));
                }
                foreach (var skillNode in Skillnodes)
                {

                    Size isize;
                    ImageBrush br = new ImageBrush();
                    Rect r = iconActiveSkills.SkillPositions[3][skillNode.Value.icon];
                    BitmapImage bi = iconActiveSkills.Images[3];
                    br.Stretch = Stretch.Uniform;
                    br.ImageSource = bi;

                    br.ViewboxUnits = BrushMappingMode.RelativeToBoundingBox;
                    br.Viewbox = new Rect(r.X / bi.PixelWidth, r.Y / bi.PixelHeight, r.Width / bi.PixelWidth, r.Height / bi.PixelHeight);
                    Vector2D pos = (skillNode.Value.Position);
                    dc.DrawEllipse(br, null, pos, r.Width, r.Height);

                }
                //   g.Dispose();
            }

            picSkillTree = drawingVisual;
            picSkillSurround = new DrawingVisual();

          
            DrawNodeSurround();
            // picLinks = new RenderTargetBitmap((int)imagesize, (int)imagesize, 96, 96,PixelFormats.Pbgra32);
            // picLinks.Clear();  

            picLinks = new DrawingVisual();

            Pen pen2 = new Pen(Brushes.DarkSlateGray, 32);
            using (DrawingContext dc = picLinks.RenderOpen())
            {
                dc.DrawGeometry(null, pen, g);
                foreach (var nid in links)
                {
                    var n1 = Skillnodes[nid[0]];
                    var n2 = Skillnodes[nid[1]];

                    if (n1.NodeGroup == n2.NodeGroup && n1.orbit == n2.orbit)
                    {

                        if (n1.Arc - n2.Arc > 0 && n1.Arc - n2.Arc < Math.PI || n1.Arc - n2.Arc < -Math.PI)
                        {
                            dc.DrawArc(null, pen2, n1.normPos, n2.normPos,
                                  new Size(SkillTree.SkillNode.orbitRadii[n1.orbit],
                                           SkillTree.SkillNode.orbitRadii[n1.orbit]));
                        }
                        else
                        {
                            dc.DrawArc(null, pen2, n2.normPos, n1.normPos,
                                  new Size(SkillTree.SkillNode.orbitRadii[n1.orbit],
                                           SkillTree.SkillNode.orbitRadii[n1.orbit]));
                        }
                    }
                    else
                    {
                        dc.DrawLine(pen2, n1.normPos, n2.normPos);
                    }
                }

            }


            // picSkillTree.Save("testtree.png");

            Regex regexAttrib = new Regex("[0-9]*\\.?[0-9]+");
            foreach (var skillNode in Skillnodes)
            {
                skillNode.Value.Attributes = new Dictionary<string, List<float>>();
                foreach (string s in skillNode.Value.attributes)
                {
                    
                    List<float> values = new List<float>();

                    foreach (Match m in regexAttrib.Matches(s))
                    {

                        if (!AttributeTypes.Contains(regexAttrib.Replace(s, "#"))) AttributeTypes.Add(regexAttrib.Replace(s, "#"));
                        if (m.Value == "") values.Add(float.NaN);
                        else values.Add(float.Parse(m.Value,System.Globalization.CultureInfo.InvariantCulture));

                    }
                    skillNode.Value.Attributes[(regexAttrib.Replace(s, "#"))] = values;


                }
            }

            picActiveLinks = new DrawingVisual();
            picPathOverlay = new DrawingVisual();


            picBackground = new DrawingVisual();
            using (DrawingContext dc = picBackground.RenderOpen())
            {
                BitmapImage[] iscr = new BitmapImage[] { assets["PSGroupBackground1"].PImage, assets["PSGroupBackground2"].PImage, assets["PSGroupBackground3"].PImage };
                Brush[] OrbitBrush = new Brush[3];
                OrbitBrush[0] = new ImageBrush(assets["PSGroupBackground1"].PImage);
                OrbitBrush[1] = new ImageBrush(assets["PSGroupBackground2"].PImage);
                OrbitBrush[2] = new ImageBrush(assets["PSGroupBackground3"].PImage);
                (OrbitBrush[2] as ImageBrush).TileMode = TileMode.FlipXY;
                (OrbitBrush[2] as ImageBrush).Viewport = new Rect(0, 0, 1, .5f);

                ImageBrush BackgroundBrush = new ImageBrush(assets["imgPanelBackground"].PImage);
                BackgroundBrush.TileMode = TileMode.FlipXY;
                dc.DrawRectangle(BackgroundBrush, null, TRect);
                foreach (var ngp in NodeGroups)
                {
                    int maxr = ngp.OcpOrb.Keys.Max(ng => int.Parse(ng));
                    if (maxr == 0) continue;
                    maxr = maxr > 3 ? 2 : maxr - 1;
                    int maxfac = maxr == 2 ? 2 : 1;
                    dc.DrawRectangle(OrbitBrush[maxr], null, new Rect(ngp.Position - new Vector2D(iscr[maxr].PixelWidth, iscr[maxr].PixelHeight * maxfac), new Size(iscr[maxr].PixelWidth * 2, iscr[maxr].PixelHeight * 2 * maxfac)));
                }
            }
            foreach (string faceName in FaceNames)
            {
                var bi = new BitmapImage(new Uri("Data\\Assets\\" + faceName + ".png", UriKind.Relative));
                FacesBrush.Add(new KeyValuePair<Rect, ImageBrush>(new Rect(0, 0, bi.PixelWidth, bi.PixelHeight), new ImageBrush(bi)));
            }
            var bi2 = new BitmapImage(new Uri("Data\\Assets\\PSStartNodeBackgroundInactive.png", UriKind.Relative));
            var bi3 = new BitmapImage(new Uri("Data\\Assets\\PSStartNodeBackgroundActive.png", UriKind.Relative));
            StartBackgrounds.Add(false, (new KeyValuePair<Rect, ImageBrush>(new Rect(0, 0, bi2.PixelWidth, bi2.PixelHeight), new ImageBrush(bi2))));
            StartBackgrounds.Add(true, (new KeyValuePair<Rect, ImageBrush>(new Rect(0, 0, bi3.PixelWidth, bi3.PixelHeight), new ImageBrush(bi3))));
            picFaces = new DrawingVisual();
            picHighlights = new DrawingVisual();



        }

        private void DrawNodeSurround()
        {
            using (DrawingContext dc = picSkillSurround.RenderOpen())
            {

                foreach (var skillNode in SkilledNodes)
                {
                    Vector2D pos = (Skillnodes[skillNode].Position);
                    if (Skillnodes[skillNode].not)
                    {
                        dc.DrawRectangle(NodeSurroundBrush[3].Value, null,
                                         new Rect((int)pos.X - NodeSurroundBrush[3].Key.Width, (int)pos.Y - NodeSurroundBrush[3].Key.Height, NodeSurroundBrush[3].Key.Width * 2,
                                                  NodeSurroundBrush[3].Key.Height * 2));
                    }
                    else if (Skillnodes[skillNode].ks)
                    {
                        dc.DrawRectangle(NodeSurroundBrush[2].Value, null,
                                         new Rect((int)pos.X - NodeSurroundBrush[2].Key.Width, (int)pos.Y - NodeSurroundBrush[2].Key.Height, NodeSurroundBrush[2].Key.Width * 2,
                                                  NodeSurroundBrush[2].Key.Height * 2));
                    }



                    dc.DrawRectangle(NodeSurroundBrush[1].Value, null,
                                     new Rect((int)pos.X - NodeSurroundBrush[1].Key.Width, (int)pos.Y - NodeSurroundBrush[1].Key.Height,
                                              NodeSurroundBrush[1].Key.Width * 2, NodeSurroundBrush[1].Key.Height * 2));

                    Size isize;
                    ImageBrush br = new ImageBrush();
                    Rect r = iconActiveSkills.SkillPositions[3][Skillnodes[skillNode].icon];
                    BitmapImage bi = iconActiveSkills.Images[3];
                    br.Stretch = Stretch.Uniform;
                    br.ImageSource = bi;

                    br.ViewboxUnits = BrushMappingMode.RelativeToBoundingBox;
                    br.Viewbox = new Rect(r.X / bi.PixelWidth, r.Y / bi.PixelHeight, r.Width / bi.PixelWidth, r.Height / bi.PixelHeight);

                    dc.DrawEllipse(br, null, pos, r.Width, r.Height);


                }
            }
        }

        public List<NodeGroup> NodeGroups = new List<NodeGroup>();
        public Dictionary<int, SkillNode> Skillnodes = new Dictionary<int, SkillNode>();
        public List<string> AttributeTypes = new List<string>();
        // public Bitmap iconActiveSkills;
        public SkillIcons iconInActiveSkills = new SkillIcons();
        public SkillIcons iconActiveSkills = new SkillIcons();
        public DrawingVisual picSkillTree;
        public DrawingVisual picSkillSurround;
        public DrawingVisual picLinks;
        public DrawingVisual picActiveLinks;
        public DrawingVisual picPathOverlay;
        public DrawingVisual picBackground;
        public DrawingVisual picFaces;
        public DrawingVisual picHighlights;
        public List<string> FaceNames = new List<string>() { "PSFaceStr", "PSFaceDex", "PSFaceInt", "PSFaceStrDex", "PSFaceStrInt", "PSFaceDexInt" };
        public Dictionary<bool, KeyValuePair<Rect, ImageBrush>> StartBackgrounds = new Dictionary<bool, KeyValuePair<Rect, ImageBrush>>();
        public List<KeyValuePair<Size, ImageBrush>> NodeSurroundBrush = new List<KeyValuePair<Size, ImageBrush>>();
        public List<KeyValuePair<Rect, ImageBrush>> FacesBrush = new List<KeyValuePair<Rect, ImageBrush>>();
        public Dictionary<string, string> nodeBackgrounds = new Dictionary<string, string>() { { "normalA", "PSSkillFrameActive" }, { "normal", "PSSkillFrame" }, { "notable", "imgNotableBackgroundNormal" }, { "keystone", "imgKeystoneBackgroundNormal" } };
        public Dictionary<string, string> nodeBackgroundsActive = new Dictionary<string, string>() { { "normal", "PSSKillFrameActive" }, { "notable", "imgNotableBackgroundHighlight" }, { "keystone", "imgKeystoneBackgroundHighlight" } };
        public List<string> CharName = new List<string>() { "MARAUDER", "RANGER", "WITCH", "DUELIST", "TEMPLAR", "SIX" };
        public Dictionary<string, float>[] CharBaseAttributes = new Dictionary<string, float>[6];
        public Dictionary<string, float> BaseAttributes = new Dictionary<string, float>()
                                                              {
                                                                  {"+# to maximum Mana",36},
                                                                  {"+# to maximum Life",45},
                                                                  {"Evasion Rating: #",50},
                                                                  {"+# Maximum Endurance Charge",2},
                                                                  {"+# Maximum Frenzy Charge",2},
                                                                  {"+# Maximum Power Charge",2},
                                                                  {"#% Additional Elemental Resistance per Endurance Charge",5},
                                                                  {"#% Physical Damage Reduction per Endurance Charge",5},
                                                                  {"#% Attack Speed Increase per Frenzy Charge",5},
                                                                  {"#% Cast Speed Increase per Frenzy Charge",5},
                                                                  {"#% Critical Strike Chance Increase per Power Charge",50},
                                                              };

        public static float LifePerLevel = 5;
        public static float ManaPerLevel = 4;
        public static float IntPerMana = 2;
        public static float IntPerES = 5; //%
        public static float StrPerLife = 2;
        public static float StrPerED = 5; //%
        public static float DexPerAcc = 0.5f;
        public static float DexPerEvas = 5; //%
        public Dictionary<string, List<float>> ImplicitAttributes(Dictionary<string, List<float>> attribs)
        {
            Dictionary<string, List<float>> retval = new Dictionary<string, List<float>>();
            // +# to Strength", co["base_str"].Value<int>() }, { "+# to Dexterity", co["base_dex"].Value<int>() }, { "+# to Intelligence", co["base_int"].Value<int>() } };
            retval["+# to maximum Mana"] = new List<float>() { attribs["+# to Intelligence"][0] / IntPerMana + level * ManaPerLevel };
            retval["+#% Energy Shield"] = new List<float>() { attribs["+# to Intelligence"][0] / IntPerES };

            retval["+# to maximum Life"] = new List<float>() { attribs["+# to Strength"][0] / IntPerMana + level * LifePerLevel };
            retval["+#% increased Melee Physical Damage"] = new List<float>() { attribs["+# to Strength"][0] / StrPerED };

            retval["+# Accuracy Rating"] = new List<float>() { attribs["+# to Dexterity"][0] / DexPerAcc };
            retval["Evasion Rating: #"] = new List<float>() { attribs["+# to Dexterity"][0] / DexPerEvas };
            return retval;
        }



        private int level = 1;
        public int Level
        {
            get { return level; }
            set { level = value; }
        }
        private int chartype = 0;
        public int Chartype
        {
            get { return chartype; }
            set
            {

                chartype = value;
                DrawFaces();
            }
        }
        private SkillNode CharacterNode;

        public HashSet<int> SkilledNodes = new HashSet<int>();
        public HashSet<int> AvailNodes = new HashSet<int>();

        public void DrawFaces()
        {
            using (DrawingContext dc = picFaces.RenderOpen())
            {
                for (int i = 0; i < CharName.Count; i++)
                {
                    var s = CharName[i];
                    var pos = Skillnodes.First(nd => nd.Value.name == s).Value.Position;
                    dc.DrawRectangle(StartBackgrounds[Chartype == i].Value, null, new Rect(pos - new Vector2D(StartBackgrounds[Chartype == i].Key.Width, StartBackgrounds[Chartype == i].Key.Height), pos + new Vector2D(StartBackgrounds[Chartype == i].Key.Width, StartBackgrounds[Chartype == i].Key.Height)));
                    dc.DrawRectangle(FacesBrush[i].Value, null, new Rect(pos - new Vector2D(FacesBrush[i].Key.Width, FacesBrush[i].Key.Height), pos + new Vector2D(FacesBrush[i].Key.Width, FacesBrush[i].Key.Height)));
                }
            }
        }
        public List<int> GetShortestPathTo(int targetNode)
        {
            if (SkilledNodes.Contains(targetNode)) return new List<int>();
            if (AvailNodes.Contains(targetNode)) return new List<int>() { targetNode };
            HashSet<int> visited = new HashSet<int>(SkilledNodes);
            Dictionary<int, int> distance = new Dictionary<int, int>();
            Dictionary<int, int> parent = new Dictionary<int, int>();
            Queue<int> newOnes = new Queue<int>();
            foreach (var node in SkilledNodes)
            {
                distance.Add(node, 0);
            }
            foreach (var node in AvailNodes)
            {
                newOnes.Enqueue(node);
                distance.Add(node, 1);
            }
            while (newOnes.Count > 0)
            {
                int newNode = newOnes.Dequeue();
                int dis = distance[newNode];
                visited.Add(newNode);
                foreach (var connection in Skillnodes[newNode].Neighbor.Select(nd => nd.id))
                {
                    if (visited.Contains(connection)) continue;
                    if (distance.ContainsKey(connection)) continue;
                    if (CharName.Contains(Skillnodes[connection].name)) continue;
                    distance.Add(connection, dis + 1);
                    newOnes.Enqueue(connection);

                    parent.Add(connection, newNode);

                    if (connection == targetNode) break;
                }
            }

            if (!distance.ContainsKey(targetNode)) return new List<int>();

            Stack<int> path = new Stack<int>();
            int curr = targetNode;
            path.Push(curr);
            while (parent.ContainsKey(curr))
            {
                path.Push(parent[curr]);
                curr = parent[curr];
            }

            List<int> result = new List<int>();
            while (path.Count > 0)
                result.Add(path.Pop());

            return result;
        }
        public void DrawPath(List<int> path)
        {
            Pen pen2 = new Pen(Brushes.LawnGreen, 15f);
            pen2.DashStyle = new DashStyle(new DoubleCollection() { 2 }, 2);

            using (DrawingContext dc = picPathOverlay.RenderOpen())
            {
                for (int i = -1; i < path.Count - 1; i++)
                {
                    SkillNode n1 = i == -1 ? Skillnodes[path[i + 1]].Neighbor.First(sn => SkilledNodes.Contains(sn.id)) : Skillnodes[path[i]];
                    SkillNode n2 = Skillnodes[path[i + 1]];

                    DrawConnection(dc, pen2, n1, n2);
                }
            }
        }
        public void DrawRefundPreview(HashSet<int> nodes)
        {
            Pen pen2 = new Pen(Brushes.Red, 15f);
            pen2.DashStyle = new DashStyle(new DoubleCollection() { 2 }, 2);

            using (DrawingContext dc = picPathOverlay.RenderOpen())
            {
                foreach (int node in nodes)
                {
                    foreach (SkillNode n2 in Skillnodes[node].Neighbor)
                    {
                        if (SkilledNodes.Contains(n2.id) && (node < n2.id || !(nodes.Contains(n2.id))))
                            DrawConnection(dc, pen2, Skillnodes[node], n2);
                    }
                }
            }

        }
        public void ClearPath()
        {
            picPathOverlay.RenderOpen().Close();
        }
        public HashSet<int> ForceRefundNodePreview(int nodeId)
        {
            if (!SkilledNodes.Remove(nodeId)) return new HashSet<int>();

            SkilledNodes.Remove(nodeId);

            HashSet<int> front = new HashSet<int>();
            front.Add(SkilledNodes.First());
            foreach (var i in Skillnodes[SkilledNodes.First()].Neighbor)
                if (SkilledNodes.Contains(i.id))
                    front.Add(i.id);

            HashSet<int> skilled_reachable = new HashSet<int>(front);
            while (front.Count > 0)
            {
                HashSet<int> newFront = new HashSet<int>();
                foreach (var i in front)
                    foreach (var j in Skillnodes[i].Neighbor.Select(nd => nd.id))
                        if (!skilled_reachable.Contains(j) && SkilledNodes.Contains(j))
                        {
                            newFront.Add(j);
                            skilled_reachable.Add(j);
                        }

                front = newFront;
            }

            HashSet<int> unreachable = new HashSet<int>(SkilledNodes);
            foreach (var i in skilled_reachable)
                unreachable.Remove(i);
            unreachable.Add(nodeId);

            SkilledNodes.Add(nodeId);

            return unreachable;
        }
        public void ForceRefundNode(int nodeId)
        {
            if (!SkilledNodes.Remove(nodeId)) throw new InvalidOperationException();

            SkilledNodes.Remove(nodeId);

            HashSet<int> front = new HashSet<int>();
            front.Add(SkilledNodes.First());
            foreach (var i in Skillnodes[SkilledNodes.First()].Neighbor)
                if (SkilledNodes.Contains(i.id))
                    front.Add(i.id);
            HashSet<int> skilled_reachable = new HashSet<int>(front);
            while (front.Count > 0)
            {
                HashSet<int> newFront = new HashSet<int>();
                foreach (var i in front)
                    foreach (var j in Skillnodes[i].Neighbor.Select(nd => nd.id))
                        if (!skilled_reachable.Contains(j) && SkilledNodes.Contains(j))
                        {
                            newFront.Add(j);
                            skilled_reachable.Add(j);
                        }

                front = newFront;
            }

            SkilledNodes = skilled_reachable;
            AvailNodes = new HashSet<int>();
            UpdateAvailNodes();
            //foreach (var i in BaseAvailableNodes[Class])
            //    if (!SkilledNodes.Contains(i))
            //        AvailableNodes.Add(i);
            //foreach (var i in SkilledNodes)
            //    foreach (var j in Skilltree.Nodes[i].Connections)
            //        if (!SkilledNodes.Contains(j))
            //            AvailableNodes.Add(j);
        }
        string TreeAddress = "http://www.pathofexile.com/passive-skill-tree/";
        public void LoadFromURL(string url)
        {
            string s = url.Substring(TreeAddress.Length).Replace("-", "+").Replace("_", "/");
            byte[] decbuff = Convert.FromBase64String(s);
            var i = BitConverter.ToInt32(new byte[] { decbuff[3], decbuff[2], decbuff[1], decbuff[1] }, 0);
            var b = decbuff[4];
            var j = 0L; if (i > 0) j = decbuff[5];
            List<int> nodes = new List<int>();
            for (int k = 6; k < decbuff.Length; k += 4)
            {
                byte[] dbff = new byte[] { decbuff[k + 3], decbuff[k + 2], decbuff[k + 1], decbuff[k + 0] };
                if (Skillnodes.Keys.Contains(BitConverter.ToInt32(dbff, 0)))
                    nodes.Add((BitConverter.ToInt32(dbff, 0)));

            }
            Chartype = b - 1;
            SkilledNodes.Clear();
            SkillTree.SkillNode startnode = Skillnodes.First(nd => nd.Value.name == CharName[Chartype].ToUpper()).Value;
            SkilledNodes.Add(startnode.id);
            foreach (int node in nodes)
            {
                SkilledNodes.Add(node);
            }
            UpdateAvailNodes();
        }
        public string SaveToURL()
        {
            byte[] b = new byte[(SkilledNodes.Count - 1) * 4 + 6];
            var b2 = BitConverter.GetBytes(1);
            b[0] = b2[3];
            b[1] = b2[2];
            b[2] = b2[1];
            b[3] = b2[0];
            b[4] = (byte)(Chartype + 1);
            b[5] = (byte)(1);
            int pos = 6;
            foreach (var inn in SkilledNodes)
            {
                if (CharName.Contains(Skillnodes[inn].name)) continue;
                byte[] dbff = BitConverter.GetBytes(inn);
                b[pos++] = dbff[3];
                b[pos++] = dbff[2];
                b[pos++] = dbff[1];
                b[pos++] = dbff[0];
            }
            return TreeAddress + Convert.ToBase64String(b).Replace("/", "_").Replace("+", "-");

        }
        public void UpdateAvailNodes()
        {
            AvailNodes.Clear();
            foreach (int inode in SkilledNodes)
            {
                SkillNode node = Skillnodes[inode];
                foreach (SkillNode skillNode in node.Neighbor)
                {
                    if (!CharName.Contains(skillNode.name) && !SkilledNodes.Contains(skillNode.id))
                        AvailNodes.Add(skillNode.id);
                }
            }
            //  picActiveLinks = new DrawingVisual();

            Pen pen2 = new Pen(Brushes.Yellow, 15f);

            using (DrawingContext dc = picActiveLinks.RenderOpen())
            {
                foreach (var n1 in SkilledNodes)
                {
                    foreach (var n2 in Skillnodes[n1].Neighbor)
                    {
                        if (SkilledNodes.Contains(n2.id))
                        {
                            DrawConnection(dc, pen2, Skillnodes[n1], n2);
                        }
                    }
                }
            }
            // picActiveLinks.Clear();
            DrawNodeSurround();
        }
        private void DrawConnection(DrawingContext dc, Pen pen2, SkillNode n1, SkillNode n2)
        {
            if (n1.NodeGroup == n2.NodeGroup && n1.orbit == n2.orbit)
            {
                if (n1.Arc - n2.Arc > 0 && n1.Arc - n2.Arc < Math.PI ||
                    n1.Arc - n2.Arc < -Math.PI)
                {
                    dc.DrawArc(null, pen2, n1.normPos, n2.normPos,
                               new Size(SkillTree.SkillNode.orbitRadii[n1.orbit],
                                        SkillTree.SkillNode.orbitRadii[n1.orbit]));
                }
                else
                {
                    dc.DrawArc(null, pen2, n2.normPos, n1.normPos,
                               new Size(SkillTree.SkillNode.orbitRadii[n1.orbit],
                                        SkillTree.SkillNode.orbitRadii[n1.orbit]));
                }
            }
            else
            {
                dc.DrawLine(pen2, n1.normPos, n2.normPos);
            }
        }
        public HashSet<int[]> Links = new HashSet<int[]>();
        public Dictionary<string, List<float>> SelectedAttributes
        {
            get
            {
                Dictionary<string, List<float>> temp = new Dictionary<string, List<float>>();
                foreach (var attr in CharBaseAttributes[Chartype])
                {
                    if (!temp.ContainsKey(attr.Key))
                        temp[attr.Key] = new List<float>();

                    if (temp.ContainsKey(attr.Key) && temp[attr.Key].Count > 0)
                        temp[attr.Key][0] += attr.Value;
                    else
                    {
                        temp[attr.Key].Add(attr.Value);
                    }
                }

                foreach (var attr in BaseAttributes)
                {
                    if (!temp.ContainsKey(attr.Key))
                        temp[attr.Key] = new List<float>();

                    if (temp.ContainsKey(attr.Key) && temp[attr.Key].Count > 0)
                        temp[attr.Key][0] += attr.Value;
                    else
                    {
                        temp[attr.Key].Add(attr.Value);
                    }
                }

                foreach (int inode in SkilledNodes)
                {
                    SkillNode node = Skillnodes[inode];
                    foreach (var attr in node.Attributes)
                    {
                        if (!temp.ContainsKey(attr.Key))
                            temp[attr.Key] = new List<float>();
                        for (int i = 0; i < attr.Value.Count; i++)
                        {

                            if (temp.ContainsKey(attr.Key) && temp[attr.Key].Count > i)
                                temp[attr.Key][i] += attr.Value[i];
                            else
                            {
                                temp[attr.Key].Add(attr.Value[i]);
                            }
                        }

                    }
                }

                foreach (var a in ImplicitAttributes(temp))
                {
                    if (!temp.ContainsKey(a.Key))
                        temp[a.Key] = new List<float>();
                    for (int i = 0; i < a.Value.Count; i++)
                    {

                        if (temp.ContainsKey(a.Key) && temp[a.Key].Count > i)
                            temp[a.Key][i] += a.Value[i];
                        else
                        {
                            temp[a.Key].Add(a.Value[i]);
                        }
                    }
                }
                return temp;
            }
        }



        public void DrawHighlights(List<SkillNode> nodes)
        {
            Pen hpen = new Pen(Brushes.Aqua, 20);
            using (DrawingContext dc = picHighlights.RenderOpen())
            {
                foreach (SkillNode node in nodes)
                {
                    dc.DrawEllipse(null, hpen, node.Position, 80, 80);
                }
            }
        }

        public Rect2D TRect = new Rect2D();
        public float scaleFactor = 1;

        public class SkillIcons
        {
            enum Detail
            {
                lowest,
                low,
                medium,
                high
            }

            public Dictionary<string, Rect>[] SkillPositions = new Dictionary<string, Rect>[4];
            public string[] Filename = new String[4];
            public BitmapImage[] Images = new BitmapImage[4];
            public static string urlpath = "http://www.pathofexile.com/image/build-gen/passive-skill-sprite/";
            public void OpenOrDownloadImages()
            {
                for (int i = 0; i < 4; i++)
                {
                    if (!File.Exists("Data\\Assets\\" + Filename[i]))
                    {
                        System.Net.WebClient _WebClient = new System.Net.WebClient();
                        _WebClient.DownloadFile(urlpath + Filename[i], "Data\\Assets\\" + Filename[i]);
                    }
                    Images[i] = new BitmapImage(new Uri("Data\\Assets\\" + Filename[i], UriKind.Relative));
                }
            }
        }

        public class NodeGroup
        {
            public Vector2D Position;// "x": 1105.14,"y": -5295.31,
            public Dictionary<string, bool> OcpOrb = new Dictionary<string, bool>(); //  "oo": {"1": true},
            public List<int> Nodes = new List<int>();// "n": [-28194677,769796679,-1093139159]

        }

        public class SkillNode
        {
            static public float[] skillsPerOrbit = { 1, 6, 12, 12, 12 };
            static public float[] orbitRadii = { 0, 81.5f, 163, 326, 489 };
            public HashSet<int> Connections = new HashSet<int>();
            public bool skilled = false;
            public int id; // "id": -28194677,
            public string icon;// icon "icon": "Art/2DArt/SkillIcons/passives/tempint.png",
            public bool ks; //"ks": false,
            public bool not;   // not": false,
            public string name;//"dn": "Block Recovery",
            public int a;// "a": 3,
            public string[] attributes;// "sd": ["8% increased Block Recovery"],
            public Dictionary<string, List<float>> Attributes;
            // public List<string> AttributeNames;
            //public List<> AttributesValues;
            public int g;// "g": 1,
            public int orbit;//  "o": 1,
            public int orbitIndex;// "oidx": 3,
            public int sa;//s "sa": 0,
            public int da;// "da": 0,
            public int ia;//"ia": 0,
            public List<int> linkID = new List<int>();// "out": []
            public List<SkillNode> Neighbor = new List<SkillNode>();
            public NodeGroup NodeGroup;
            public Vector2D normPos;
            public double arc
            {
                get
                {
                    Vector2D v = normPos - NodeGroup.Position;
                    return Math.Atan2(v.Y, v.X);
                }
            }
            public Vector2D Position
            {
                get
                {
                    double d = orbitRadii[this.orbit];
                    double b = (2 * Math.PI * this.orbitIndex / skillsPerOrbit[this.orbit]);
                    return (NodeGroup.Position - new Vector2D(d * Math.Sin(-b), d * Math.Cos(-b)));
                }
            }
            public double Arc
            {
                get { return (2 * Math.PI * this.orbitIndex / skillsPerOrbit[this.orbit]); }
            }
        }


        public class Asset
        {
            public string Name;
            public BitmapImage PImage;
            public string URL;
            public Asset(string name, string url)
            {
                Name = name;
                URL = url;
                if (!File.Exists("Data\\Assets\\" + Name + ".png"))
                {

                    System.Net.WebClient _WebClient = new System.Net.WebClient();
                    _WebClient.DownloadFile(URL, "Data\\Assets\\" + Name + ".png");



                }
                PImage = new BitmapImage(new Uri("Data\\Assets\\" + Name + ".png", UriKind.Relative));

            }

        }
        Dictionary<string, Asset> assets = new Dictionary<string, Asset>();



    }


}
