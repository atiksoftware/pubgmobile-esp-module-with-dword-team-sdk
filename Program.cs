using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using FlatPMSDK;
using GameOverlay.Drawing;
using GameOverlay.Windows;
using SharpDX;
using Point = GameOverlay.Drawing.Point;
using Color = GameOverlay.Drawing.Color;
using Rectangle = GameOverlay.Drawing.Rectangle;
using System.Windows.Forms; 
using System.Collections; 
using System.Reflection;
using System.Text;
using Newtonsoft.Json;
using System.Windows.Media;
using System.Globalization;
using System.Timers;
using SharpDX.DirectWrite;

namespace ESPX
{
 



    class Program : Instance
    {
        public override PluginInfo info { get => new PluginInfo() {
                author = "Amiral Router",
                name = "ESPX",
                descripton="Its Custom ESP. But its getting colors and names from file",
                version = 1003
            };
        }

        private static FlatSDKInternal.Entity LocalPlayer = null;
        public enum KAccessModifier
        {
            Bones = 1 << 0,
            Angles = 1 << 1,
            Items = 1 << 2,
            Cars = 1 << 3,
            PlayerNick = 1 << 4,
            Inventory = 1 << 5
        }
        public static class ESPXOptions
        {
            public static int bESPXFolder = 0;
            public static int bLockInScreen = 1;
            public static int bShowAllItems = 1;
            public static int bFixOverview = 1;
            public static int bTextWithBg = 1;

            public static int bShowDistance = 1;
            public static int bSnapLines = 1;
            public static int bShowBox = 1;


            [NonSerialized]
            public static int bLoadColorsBTN = 0;
            [NonSerialized]
            public static int bSaveBTN = 0;
            [NonSerialized]
            public static int bLoadBTN = 0;
        }

        public static Dictionary<string, lootItem> lootItems;
        public class lootItem
        { 
            public string id;
            public string name;

            public int[] color = new int[4];
            public IBrush fontBrush;

            public int[] bgColor = new int[4];
            public IBrush bgBrush;
            public int fontSize;
            public float textWidth = 0.0f;
            public float textHeight = 0.0f;
            public bool view = true;
        } 


        public List<float[]> entityDrawPositions = new List<float[]>() { };
        public bool isCollision(float al, float at, float ar, float ab, float bl, float bt, float br, float bb)
        {
            if(bl > ar || al > br || bt > ab || at > bb)
            {
                return false;
            }
            return true;
        }
        public float findFreeAreaForDraw(float l = 0f,float t = 0f, float r = 0f, float b = 0f, int deep = 0)
        {

            float w = r - l;
            float h = b - t;
            float cx = l + w / 2;
            float cy = t + h / 2;
            foreach (var usedPos in this.entityDrawPositions)
            {
                if ( isCollision(l, t, r, b, usedPos[0], usedPos[1], usedPos[2], usedPos[3]) )
                {
                    if(deep < 64) {
                       return findFreeAreaForDraw(l, usedPos[1] - h - 2f, r, usedPos[1] - 2f,deep + 1);
                    }
                } 
            } 
            return t;
        }

        
        private void DrawItemWeapon(GameOverlay.Drawing.Graphics gfx, FlatSDKInternal.Entity go, string itemId)
        {

            FlatSDK.WorldToScreen(FlatSDK.Overlay.Width, FlatSDK.Overlay.Height, go.extra.FootPos, out Vector2 MaxOutput);
            bool w2s = FlatSDK.WorldToScreen(FlatSDK.Overlay.Width, FlatSDK.Overlay.Height, go.position, out Vector2 MinOutput);
             
            float x = MinOutput.X - lootItems[itemId].fontSize - 10;
            float y = MinOutput.Y;

            if (ESPXOptions.bFixOverview == 1) {
                y = findFreeAreaForDraw(x, y, x + lootItems[itemId].textWidth + 8f, y + lootItems[itemId].fontSize + 8f);
            }

            float padding = 0f;
            if (ESPXOptions.bTextWithBg == 1) {
                padding = 8f;
            }

            float r = x + lootItems[itemId].textWidth + padding;
            float b = y + lootItems[itemId].fontSize + padding;

            
            if (ESPXOptions.bLockInScreen == 1) {
                if (x < 0) { x = 0; }
                if (y < 0) { y = 0; }
                if (r > FlatSDK.Overlay.Width) { x = FlatSDK.Overlay.Width + x - r; }
                if (b > FlatSDK.Overlay.Height) { y = FlatSDK.Overlay.Height + y - b; }
            }

                try {
                    if (ESPXOptions.bShowAllItems == 1 || lootItems[itemId].view) {
                        if (ESPXOptions.bTextWithBg == 1) {
                            gfx.DrawTextWithBackground(FlatSDKInternal.IRenderer._font, lootItems[itemId].fontSize, lootItems[itemId].fontBrush, lootItems[itemId].bgBrush, x, y, lootItems[itemId].name.ToString());
                        }
                        else {
                            gfx.DrawText(FlatSDKInternal.IRenderer._font, lootItems[itemId].fontSize, lootItems[itemId].fontBrush, x, y, lootItems[itemId].name.ToString());
                        } 
                    } 
                    if (ESPXOptions.bFixOverview == 1) {
                        entityDrawPositions.Add(new float[] { x, y, r, b });
                    } 
                }  catch { }
             

        }


        public override async Task Load()
        {
            FlatSDK.DrawGraphics += FlatSDK_DrawGraphics;
            FlatSDK.SetupGraphics += FlatSDK_SetupGraphics;

            FlatSDKInternal.SetSDKModifier(FlatSDKInternal.KAccessModifier.Items);

            var ESPXFolderObject = FlatSDK.menuBase.AddFolderElement(ref ESPXOptions.bESPXFolder, "ESPX Options", FlatSDKInternal.folderonoff);
             
            FlatSDK.menuBase.AddTextElement(ref ESPXOptions.bLockInScreen, "Jail Items In Screen", FlatSDKInternal.onoff, ESPXFolderObject);
            FlatSDK.menuBase.AddTextElement(ref ESPXOptions.bShowAllItems, "Force Show All Items", FlatSDKInternal.onoff, ESPXFolderObject);
            FlatSDK.menuBase.AddTextElement(ref ESPXOptions.bFixOverview, "Fix Overview", FlatSDKInternal.onoff, ESPXFolderObject);
            FlatSDK.menuBase.AddTextElement(ref ESPXOptions.bTextWithBg, "Background Color Active", FlatSDKInternal.onoff, ESPXFolderObject);

            //FlatSDK.menuBase.AddTextElement(ref ESPXOptions.bShowBox, "Box ESP", FlatSDKInternal.onoff, ESPXFolderObject);
            //FlatSDK.menuBase.AddTextElement(ref ESPXOptions.bShowDistance, "Distance ESP", FlatSDKInternal.onoff, ESPXFolderObject);

            FlatSDK.menuBase.AddTextElement(ref ESPXOptions.bLoadColorsBTN, "Reload Config File", FlatSDKInternal.savebtn, ESPXFolderObject);
            FlatSDK.menuBase.AddTextElement(ref ESPXOptions.bSaveBTN, "Save", FlatSDKInternal.savebtn, ESPXFolderObject);
            FlatSDK.menuBase.AddTextElement(ref ESPXOptions.bLoadBTN, "Load", FlatSDKInternal.loadbtn, ESPXFolderObject);


            SerializeStatic.Deserialize(typeof(ESPXOptions), "ESPXCFG.xml");



        }
          
        private void FlatSDK_SetupGraphics(object sender, SetupGraphicsEventArgs e)
        {
            var gfx = e.Graphics;

            gfx.TextAntiAliasing = true;

            loadColorsFromFile(gfx);

            Console.Write("Creating Timer \n");
            timerEntityItemsLoader = new System.Timers.Timer(1000);
            timerEntityItemsLoader.Elapsed += loadEntities;
            timerEntityItemsLoader.AutoReset = true;
            timerEntityItemsLoader.Enabled = true;
            timerEntityItemsLoader.Stop();
            timerEntityItemsLoader.Start();

        }
        public void loadColorsFromFile(Graphics gfx)
        {
            using (StreamReader r = new StreamReader("ESPWeapons.json"))
            {
                string json = r.ReadToEnd();

                var dwFactory = new SharpDX.DirectWrite.Factory();
                var textFormat = new TextFormat(dwFactory, "Arial", 10) { };


                lootItems = JsonConvert.DeserializeObject<Dictionary<string, lootItem>>(json);
                foreach (var lootItem in lootItems)
                {
                    lootItem.Value.fontBrush = gfx.CreateSolidBrush((float)lootItem.Value.color[0], (float)lootItem.Value.color[1], (float)lootItem.Value.color[2], (float)lootItem.Value.color[3] / 255);
                    lootItem.Value.bgBrush = gfx.CreateSolidBrush((float)lootItem.Value.bgColor[0], (float)lootItem.Value.bgColor[1], (float)lootItem.Value.bgColor[2], (float)lootItem.Value.bgColor[3] / 255);

                    var textLayout = new TextLayout(dwFactory, lootItem.Value.name.ToString(), textFormat, float.PositiveInfinity, float.PositiveInfinity);
                    lootItem.Value.textWidth = textLayout.Metrics.Width;
                }
            }
        }
        private System.Timers.Timer timerEntityItemsLoader;
        public IEnumerable<FlatSDKInternal.Entity> listEntityItems = new List<FlatSDKInternal.Entity>() {  };
        public IEnumerable<FlatSDKInternal.Entity> listEntityCars = new List<FlatSDKInternal.Entity>() {  };
        public void loadEntities(Object source, ElapsedEventArgs e)
        {
            Console.Write("Loading Entities All \n"); 
            Console.Write(entityDrawPositions.Count  + "  \n"); 
            listEntityItems = FlatSDK.GetItems();
            listEntityCars = FlatSDK.GetCars();
            timerEntityItemsLoader.Interval = 1000;
        }


        private void FlatSDK_DrawGraphics(object sender, DrawGraphicsEventArgs e)
        {
            var gfx = e.Graphics;

            if (ESPXOptions.bLoadColorsBTN == 1)
            {
                loadColorsFromFile(gfx);
                ESPXOptions.bLoadColorsBTN = 0;
            }
            if (ESPXOptions.bSaveBTN == 1)
            {
                SerializeStatic.Serialize(typeof(ESPXOptions), "ESPXCFG.xml");
                ESPXOptions.bSaveBTN = 0;
            }
            if (ESPXOptions.bLoadBTN == 1)
            {
                SerializeStatic.Deserialize(typeof(ESPXOptions), "ESPXCFG.xml");
                ESPXOptions.bLoadBTN = 0;
            }


            LocalPlayer = FlatSDK.GetLocalPlayer();

            if (LocalPlayer == null)
                return;
            if (LocalPlayer.extra == null)
                return;
             


            if (entityDrawPositions.Count > 0)
            {
                entityDrawPositions.Clear();
            }
             
            foreach (var entity in listEntityItems)
            {
                //var dump = ObjectDumper.Dump(entity.eItem); 
                //var dump = ObjectDumper.Dump(entity); 

                //if (entity.type != 82) { continue; }

                // Console.Write("\n!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!n");
                //Console.Write(dump); 
                //
                //myarray.Add(entity.eItem.id.ToString()  +  ":" + entity.eItem.gname.ToString() );
                try
                { 
                    DrawItemWeapon(gfx, entity, entity.eItem.id.ToString());
                }
                catch {
                    Console.Write("DrawItemWeapon error\n"); 
                }

            } 
        }


    }
}
