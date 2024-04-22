using System;
using System.Threading.Tasks;
using CitizenFX.Core;
using static CitizenFX.Core.Native.API;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
//using MenuAPI;
//using Newtonsoft.Json;
using CitizenFX.Core;
using CitizenFX.Core.UI;
using static CitizenFX.Core.UI.Screen;
using static CitizenFX.Core.Native.API;


using System.IO;


namespace DatasetGenerator.Client
{
    public class ClientMain : BaseScript
    {
        

        public int bbMod = 1;
        private bool debugMode = false;
        private bool showBoxMode = false;
        private bool collectMode = false;
        private bool dashcamMode = false;
        private int currentID = 0;
        public static int getInfoKey = 170; //f3
        public static int debugKey = 166; //f5
        public static int showBoxKey = 167; //f6
        public static int collectKey = 288; //f1
        public static int carOnlyCollectKey = 168; //f8
        public static int dashcamCollectKey = 111; //Num 8
        internal static float entityRange = 7500f;//10000f;//15000f;
        public float targetSpeed = 30f;
        private int picsPerCondition = 50;
        private int ticksBetweenPicsBackup = 90;
        private int ticksBetweenPics = 90;
        private int ticksBetweenPicsDashcam = 120;
        Random random = new Random();

        
        //string saveDir = @"F:\Programming\Dissertation-mk2\dataset\";
        string saveDir = @"D:\Dissertation\dataset\";
        

        private List<Vehicle> vehicles = new List<Vehicle>();
        private Vector3 playerPos;
        private string filePath;
        private List<string> metadataList;
        private int tickCounter = 0;
        private string metadataString = "";
        private bool saveMetadata = false;
        private bool vehiclesFrozen = false;

        private bool canEnd = false;
        private bool takingData = false;
        private DateTime start;
        private List<Vector3> vehicleVelocities;
        private Dictionary<int, Vector3> carVelocityDict ;
        private bool canStart = false;
        private Location location1;
        private Location location2;
        private bool vehiclesOnScreen=false;
        Vehicle playerVehicle = null;
        //private List<Location> locations;
        private Location[] locations;
        private int currentLocationIndex = 0;

        private bool generateTestDataset=true;

         string[] vehicleNames = {
            "adder", "cheetah", "entityxf", "zentorno", "t20", //Super
            "seminole", "rocoto", "gresley", "baller", "baller2", //SUV
            "burrito", "rumpo", "pony", "speedo", "youga", //vans
            "rapidgt", "carbonizzare", "banshee", "massacro", "pariah", // sports
            "asterope", "intruder", "primo", "stanier", "schafter2", //Sedan
            "dominator", "gauntlet", "vigero", "stalion", "dominator3", //Muscle
            "kamacho", "rebel2", "sandking2", "mesa3", "rancherxl", //off-road
            "bati", "sanchez2", "hakuchou", "zombieb", "fcr", //Motorbikes
            "tribike3", "scorcher", "bmx", "fixter", "cruiser", //Cycles
            "oracle", "felon", "jackal", "sentinel2", "zion", //coupe
            "phantom", "benson", "mule", "biff", "stockade", //commercial 
            "issi3", "brioso", "rhapsody", "panto", "issi2", // compacts
        
        
        };
        /*private int[] times = {0,6,12,18};
        string[] weathers = {"CLEAR", "RAIN", "FOGGY", "SNOW"};*/
        


        #region Drawing text on screen
        public static void DrawTextOnScreen(string text, float xPosition, float yPosition, float size, CitizenFX.Core.UI.Alignment justification, int font, bool disableTextOutline)
        {
            
            SetTextFont(font);
            SetTextScale(1.0f, size);
            if (justification == CitizenFX.Core.UI.Alignment.Right)
            {
                SetTextWrap(0f, xPosition);
            }
            SetTextJustification((int)justification);
            if (!disableTextOutline) { SetTextOutline(); }
            BeginTextCommandDisplayText("STRING");
            AddTextComponentSubstringPlayerName(text);
            EndTextCommandDisplayText(xPosition, yPosition);
            
        }
        
        /// <summary>
        /// Draw text on the screen at the provided x and y locations.
        /// </summary>
        /// <param name="text">The text to display.</param>
        /// <param name="xPosition">The x position for the text draw origin.</param>
        /// <param name="yPosition">The y position for the text draw origin.</param>
        /// <param name="size">The size of the text.</param>
        /// <param name="justification">Align the text. 0: center, 1: left, 2: right</param>
        /// <param name="font">Specify the font to use (0-8).</param>
        public static void DrawTextOnScreen(string text, float xPosition, float yPosition, float size, CitizenFX.Core.UI.Alignment justification, int font) =>
            DrawTextOnScreen(text, xPosition, yPosition, size, justification, font, false);

        #endregion

        #region Bounding Boxes
            
        /// <summary>
        /// Gets the bounding box of the entity model in world coordinates, used by <see cref="DrawEntityBoundingBox(Entity, int, int, int, int)"/>.
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        internal static Vector3[] GetEntityBoundingBox(int entity)
        {
            Vector3 min = Vector3.Zero;
            Vector3 max = Vector3.Zero;

            GetModelDimensions((uint)GetEntityModel(entity), ref min, ref max);
            const float padBottom = -0.1f;
            const float pad = 0f;
            var retval = new Vector3[8]
            {
                // Bottom
                GetOffsetFromEntityInWorldCoords(entity, min.X - pad, min.Y - pad, min.Z - pad- padBottom),
                GetOffsetFromEntityInWorldCoords(entity, max.X + pad, min.Y - pad, min.Z - pad- padBottom),
                GetOffsetFromEntityInWorldCoords(entity, max.X + pad, max.Y + pad, min.Z - pad- padBottom),
                GetOffsetFromEntityInWorldCoords(entity, min.X - pad, max.Y + pad, min.Z - pad- padBottom),

                // Top
                GetOffsetFromEntityInWorldCoords(entity, min.X - pad, min.Y - pad, max.Z + pad),
                GetOffsetFromEntityInWorldCoords(entity, max.X + pad, min.Y - pad, max.Z + pad),
                GetOffsetFromEntityInWorldCoords(entity, max.X + pad, max.Y + pad, max.Z + pad),
                GetOffsetFromEntityInWorldCoords(entity, min.X - pad, max.Y + pad, max.Z + pad)
            };

            return retval;
        }

         /// <summary>
        /// Draws the edge poly faces and the edge lines for the specific box coordinates using the specified rgba color.
        /// </summary>
        /// <param name="box"></param>
        /// <param name="r"></param>
        /// <param name="g"></param>
        /// <param name="b"></param>
        /// <param name="a"></param>
        private static void DrawBoundingBox(Vector3[] box, int r, int g, int b, int a)
        {
            var polyMatrix = GetBoundingBoxPolyMatrix(box);
            var edgeMatrix = GetBoundingBoxEdgeMatrix(box);

            DrawPolyMatrix(polyMatrix, r, g, b, a);
            DrawEdgeMatrix(edgeMatrix, 255, 255, 255, 255);
        }

        /// <summary>
        /// Gets the coordinates for all poly box faces.
        /// </summary>
        /// <param name="box"></param>
        /// <returns></returns>
        private static Vector3[][] GetBoundingBoxPolyMatrix(Vector3[] box)
        {
            return new Vector3[12][]
            {
                new Vector3[3] { box[2], box[1], box[0] },
                new Vector3[3] { box[3], box[2], box[0] },

                new Vector3[3] { box[4], box[5], box[6] },
                new Vector3[3] { box[4], box[6], box[7] },

                new Vector3[3] { box[2], box[3], box[6] },
                new Vector3[3] { box[7], box[6], box[3] },

                new Vector3[3] { box[0], box[1], box[4] },
                new Vector3[3] { box[5], box[4], box[1] },

                new Vector3[3] { box[1], box[2], box[5] },
                new Vector3[3] { box[2], box[6], box[5] },

                new Vector3[3] { box[4], box[7], box[3] },
                new Vector3[3] { box[4], box[3], box[0] }
            };
        }

        /// <summary>
        /// Gets the coordinates for all edge coordinates.
        /// </summary>
        /// <param name="box"></param>
        /// <returns></returns>
        private static Vector3[][] GetBoundingBoxEdgeMatrix(Vector3[] box)
        {
            return new Vector3[12][]
            {
                new Vector3[2] { box[0], box[1] },
                new Vector3[2] { box[1], box[2] },
                new Vector3[2] { box[2], box[3] },
                new Vector3[2] { box[3], box[0] },

                new Vector3[2] { box[4], box[5] },
                new Vector3[2] { box[5], box[6] },
                new Vector3[2] { box[6], box[7] },
                new Vector3[2] { box[7], box[4] },

                new Vector3[2] { box[0], box[4] },
                new Vector3[2] { box[1], box[5] },
                new Vector3[2] { box[2], box[6] },
                new Vector3[2] { box[3], box[7] }
            };
        }

        public static void DrawEntityBoundingBox(Entity ent, int r, int g, int b, int a)
        {
            // list of length 8 for all the corners
            var box = GetEntityBoundingBox(ent.Handle);
            DrawBoundingBox(box, r, g, b, a);
        }

         /// <summary>
        /// Draws the poly matrix faces.
        /// </summary>
        /// <param name="polyCollection"></param>
        /// <param name="r"></param>
        /// <param name="g"></param>
        /// <param name="b"></param>
        /// <param name="a"></param>
        private static void DrawPolyMatrix(Vector3[][] polyCollection, int r, int g, int b, int a)
        {
            foreach (var poly in polyCollection)
            {
                float x1 = poly[0].X;
                float y1 = poly[0].Y;
                float z1 = poly[0].Z;

                float x2 = poly[1].X;
                float y2 = poly[1].Y;
                float z2 = poly[1].Z;

                float x3 = poly[2].X;
                float y3 = poly[2].Y;
                float z3 = poly[2].Z;
                DrawPoly(x1, y1, z1, x2, y2, z2, x3, y3, z3, r, g, b, a);
            }
        }

        /// <summary>
        /// Draws the edge lines for the model dimensions.
        /// </summary>
        /// <param name="linesCollection"></param>
        /// <param name="r"></param>
        /// <param name="g"></param>
        /// <param name="b"></param>
        /// <param name="a"></param>
        private static void DrawEdgeMatrix(Vector3[][] linesCollection, int r, int g, int b, int a)
        {
            foreach (var line in linesCollection)
            {
                float x1 = line[0].X;
                float y1 = line[0].Y;
                float z1 = line[0].Z;

                float x2 = line[1].X;
                float y2 = line[1].Y;
                float z2 = line[1].Z;

                DrawLine(x1, y1, z1, x2, y2, z2, r, g, b, a);
            }
        }


        #endregion

        public ClientMain()
        {
            
            Debug.WriteLine($" DatasetGenerator.Client activated at [{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}]");

            bool testMode = false;
            picsPerCondition =50;
            locations = new Location[]{
                //new Location(new Vector3(-106.6908f, -519.0898f, 39.84289f), 0f, new Vector3(0f, 0f, -8.321015f), -30.22839f, 10f, picsFromLocation, true),

                 // Highways
                new Location(new Vector3(-1789.430f, -650.141f, 20f), 5.567f, new Vector3(0.000f, 0.000f, 5.567f),-17.59277f, 30f, picsPerCondition, testMode),
                new Location(new Vector3(-2681.45f, 2616.035f, 19.39864f), -6.830189E-06f, new Vector3(0f, 0f, -47.18701f), -2.454675f, 30f, picsPerCondition, testMode),
                new Location(new Vector3(-1162.208f, 5250.1f, 75.11191f), 6.830189E-06f, new Vector3(0f, 0f, 71.09592f), -31.23303f, 30f, picsPerCondition, testMode),
                new Location(new Vector3(2609.129f, 5170.345f, 69.45427f), -1.366038E-05f, new Vector3(0f, 0f, -171.2556f), -35.15601f, 10f, picsPerCondition, testMode),
                new Location(new Vector3(2437.065f, 2930.025f, 53.70098f), 1.366038E-05f, new Vector3(0f, 0f, 127.0705f), -29.16621f, 10f, picsPerCondition, testMode),//new Location(new Vector3(2432.474f, 2928.731f, 57.77627f), 1.366038E-05f, new Vector3(0f, 0f, 128.9702f), -31.85179f, 10f, picsPerCondition, testMode),
                // Urban
                new Location(new Vector3(1769.527f, 3540.025f, 36.594f), 339.961f, new Vector3(0.000f, 0.000f, -20.039f),-1.670777f, 10f, picsPerCondition, testMode),
                new Location(new Vector3(-640.952f, -1531.846f, 22.94387f), 6.830189E-06f, new Vector3(0f, 0f, 103.7186f), -10.77995f, 10f, picsPerCondition, testMode), //slow
                new Location(new Vector3(-294.5515f, -1821.041f, 49.27009f), -6.830189E-06f, new Vector3(0f, 0f, -104.069f), -20.70007f, 10f, picsPerCondition, testMode),
                new Location(new Vector3(81.70882f, -1906.543f, 25.84157f), 3.415094E-06f, new Vector3(0f, 0f, 42.32573f), -6.533714f, 10f, picsPerCondition, testMode),
                new Location(new Vector3(910.7935f, -2470.437f, 28.65929f), 0f, new Vector3(0f, 0f, 41.96225f), 1.877675f, 10f, picsPerCondition, testMode),
                
                new Location(new Vector3(1389.423f, -1631.4f, 63.67603f), 6.830189E-06f, new Vector3(0f, 0f, 75.31506f), -13.27346f, 10f, picsPerCondition, testMode),
                new Location(new Vector3(1134.769f, -523.7743f, 70.09505f), -0.08037766f, new Vector3(0f, 0f, -81.28288f), -7.439734f, 10f, picsPerCondition, testMode),//new Location(new Vector3(1125.738f, -513.2305f, 72.53145f), -6.830189E-06f, new Vector3(0f, 0f, -98.63248f), -6.08167f, 10f, picsPerCondition, testMode),
                new Location(new Vector3(1065.598f, -534.1071f, 64.73195f), 6.830189E-06f, new Vector3(0f, 0f, 97.7282f), -3.598453f, 10f, picsPerCondition, testMode),
                new Location(new Vector3(863.1425f, -511.2666f, 58.13159f), -1.366038E-05f, new Vector3(0f, 0f, -139.1936f), 2.177621f, 10f, picsPerCondition, testMode),
                new Location(new Vector3(548.1376f, -189.2076f, 79.43042f), 6.830189E-06f, new Vector3(0f, 0f, 58.4398f), -21.22769f, 10f, picsPerCondition, testMode),
                
                new Location(new Vector3(367.6646f, 116.7666f, 109.1424f), 1.366038E-05f, new Vector3(0f, 0f, 119.7441f), -19.6664f, 10f, picsPerCondition, testMode),
                new Location(new Vector3(199.72f, 211.8542f, 109.1424f), 6.830189E-06f, new Vector3(0f, 0f, 103.8686f), -4.056938f, 10f, picsPerCondition, testMode),
                new Location(new Vector3(-917.3553f, 222.5634f, 70.46163f), -4.268868E-07f, new Vector3(0f, 0f, -5.603947f), -1.524998f, 10f, picsPerCondition, testMode),
                new Location(new Vector3(-1812.643f, 741.5195f, 147.9204f), 6.830189E-06f, new Vector3(0f, 0f, 93.5533f), -13.79819f, 10f, picsPerCondition, testMode),
                new Location(new Vector3(-1561.443f, 2141.757f, 63.60139f), -6.830189E-06f, new Vector3(0f, 0f, -95.46081f), -4.624403f, 10f, picsPerCondition, testMode),

                // New City locations
                new Location(new Vector3(-396.6956f, 258.3295f, 84.95533f), -0.08379275f, new Vector3(0f, 0f, -169.8985f), -9.746197f, 10f, picsPerCondition, testMode),
                new Location(new Vector3(-684.6088f, -1201.297f, 11.48662f), 6.830189E-06f, new Vector3(0f, 0f, 99.10529f), -4.075363f, 10f, picsPerCondition, testMode),
                new Location(new Vector3(-1099.225f, -1308.184f, 6.221095f), 1.366038E-05f, new Vector3(0f, 0f, 114.9308f), 0.3984259f, 10f, picsPerCondition, testMode),
                new Location(new Vector3(-996.4599f, -856.6437f, 14.02444f), -0.08833483f, new Vector3(0f, 0f, 69.45237f), -6.899021f, 10f, picsPerCondition, testMode),
                new Location(new Vector3(-1123.72f, -1337.818f, 11.41242f), 3.415094E-06f, new Vector3(0f, 0f, 51.71848f), -12.19378f, 10f, picsPerCondition, testMode),//new Location(new Vector3(-1013.158f, -852.0976f, 17.15666f), 0f, new Vector3(0f, 0f, -29.30029f), -18.85762f, 10f, picsPerCondition, testMode),
                
                new Location(new Vector3(-924.1362f, -543.504f, 25.23962f), -8.537736E-07f, new Vector3(0f, 0f, -14.02249f), -19.26244f, 10f, picsPerCondition, testMode),
                new Location(new Vector3(-941.0885f, -554.3464f, 33.30359f), 6.830189E-06f, new Vector3(0f, 0f, 110.3936f), -39.78363f, 10f, picsPerCondition, testMode),
                new Location(new Vector3(-1087.42f, -669.5455f, 31.80692f), 6.830189E-06f, new Vector3(0f, 0f, 90.27394f), -27.87856f, 10f, picsPerCondition, testMode),//new Location(new Vector3(-1099.231f, -673.6322f, 25.81387f), 1.366038E-05f, new Vector3(0f, 0f, 135.4087f), -13.31058f, 10f, picsPerCondition, testMode),  
                new Location(new Vector3(54.78693f, -761.6329f, 48.47496f), -0.08438015f, new Vector3(0f, 0f, 87.01579f), -18.66828f, 10f, picsPerCondition, testMode),//new Location(new Vector3(-1098.779f, -729.7245f, 24.64202f), 0f, new Vector3(0f, 0f, 41.71575f), -26.91467f, 10f, picsPerCondition, testMode),
                new Location(new Vector3(-801.694f, -78.85551f, 61.41818f), -3.415094E-06f, new Vector3(0f, 0f, -48.00604f), -51.43717f, 10f, picsPerCondition, testMode),
                
                new Location(new Vector3(-781.9294f, 217.2976f, 77.67545f), 0f, new Vector3(0f, 0f, -100.372f), -24.58274f, 10f, picsPerCondition, testMode),
                new Location(new Vector3(2437.065f, 2930.025f, 53.70098f), 1.366038E-05f, new Vector3(0f, 0f, 127.0705f), -29.16621f, 10f, picsPerCondition, testMode),//new Location(new Vector3(241.6309f, 305.2818f, 106.5246f), 0f, new Vector3(0f, 0f, -16.41452f), -9.46713f, 10f, picsPerCondition, testMode),
                new Location(new Vector3(-99.78148f, -124.4792f, 66.23969f), -3.415094E-06f, new Vector3(0f, 0f, -34.13422f), -24.21855f, 10f, picsPerCondition, testMode),//new Location(new Vector3(-77.56629f, -68.11481f, 75.74409f), -1.366038E-05f, new Vector3(0f, 0f, -177.6649f), -39.62478f, 10f, picsPerCondition, testMode),
                new Location(new Vector3(-325.806f, -885.0453f, 48.75237f), -3.415094E-06f, new Vector3(0f, 0f, -44.97601f), -25.67531f, 10f, picsPerCondition, testMode),
                new Location(new Vector3(361.7188f, -1971.461f, 29.44312f), 0f, new Vector3(0f, 0f, -32.37278f), -6.740258f, 10f, picsPerCondition, testMode),
            };
            
            TriggerServerEvent("generateDirs", $"{saveDir}\\images", $"{saveDir}\\labels");

            EventHandlers["updateMetadata"] += new Action<bool> (UpdateMetadata);
            EventHandlers["saveMetadata"] += new Action (SaveMetadata);
            Tick += OnTick;
        }


        private async void FreezeVehicles(bool freezeMode){

            Vehicle[] allNearbyCars = World.GetAllVehicles();
            

            if(freezeMode){
                vehiclesFrozen=true;

                vehicleVelocities = new List<Vector3>{};
                carVelocityDict = new Dictionary<int, Vector3>();

                foreach (Vehicle v in allNearbyCars){
                    int currentVeh = v.Handle;
                    Vector3 currentVel = GetEntityVelocity(currentVeh);
                    if((currentVel==new Vector3(0,0,0) || currentVel.Length()<targetSpeed/4) && !dashcamMode){ //Target speed /6   targetSpeed/4 7.5f currentVeh!=playerVehicle.Handle
                        if(DoesEntityExist(v.Driver.Handle)){
                                                            v.Driver.Delete();
                                                        }
                        DeleteEntity(ref currentVeh);
                       
                        
                        //v.IsVisible = false;

                    }else{
                        if (playerVehicle!=null){
                            if (v.Handle!=playerVehicle.Handle){
                            v.IsVisible = true;
                            }
                        }
                        carVelocityDict.Add(currentVeh, currentVel);
                        //Debug.WriteLine($"Freezing");
                        FreezeEntityPosition(currentVeh, true);
                    }
                    
                 }    
            }else{
                //Debug.WriteLine($"hello there");
                foreach(KeyValuePair<int, Vector3> item in carVelocityDict){
                        
                        int currentVehicle = item.Key;
                        Vehicle v = new Vehicle(currentVehicle);
                        
                        bool canDo = true;

                        if (playerVehicle!=null){
                                if (v.Handle==playerVehicle.Handle){
                                    canDo = false;
                                }
                        }


                        if(v.Position.DistanceToSquared(playerPos) < entityRange && HasEntityClearLosToEntity(PlayerPedId(), v.Handle, 17)&&canDo){

                            int randomIndex = random.Next(0, vehicleNames.Length);
                            var vehicleHash = (uint)GetHashKey(vehicleNames[randomIndex]);

                            RequestModel(vehicleHash);
                            while (!HasModelLoaded(vehicleHash)){
                                await Delay(0);
                            }

                            var driver = v.Driver;

                            var pos = GetEntityCoords(driver.Handle, false);
                            var heading = GetEntityHeading(driver.Handle);

                            v.Driver.Delete();
                            v.Delete();

                            var vehicle = new Vehicle(CreateVehicle(vehicleHash, pos.X, pos.Y, pos.Z, heading, true, false)){
                                NeedsToBeHotwired = false,
                                IsEngineRunning = true
                            };

                            vehicle.PlaceOnGround();

                            if(DoesEntityExist(vehicle.Driver.Handle)){
                                vehicle.Driver.Delete();
                            }

                            if(vehicle.IsSeatFree(VehicleSeat.Driver)){
                                RequestModel((uint)0x62018559);
                                vehicle.CreatePedOnSeat(VehicleSeat.Driver, new Model(PedHash.AirworkerSMY));
                            }

                            Ped driverPedGuy = vehicle.Driver;
                            ClearPedTasks(driverPedGuy.Handle);

                            //var veh = driver.CurrentVehicle;
                            var model = (uint)vehicle.Model.Hash;

                            SetDriverAbility(driverPedGuy.Handle, 1f);
                            SetDriverAggressiveness(driverPedGuy.Handle, 0f);

                            TaskVehicleDriveWander(driverPedGuy.Handle, vehicle.Handle, GetVehicleModelMaxSpeed(model), 443);
                            Vector3 currentVelocity = item.Value;
                            FreezeEntityPosition(vehicle.Handle, false);
                            float currentSpeed = currentVelocity.Length();

                            if(currentVelocity.Length()<targetSpeed){
                                float modifier = 1+(targetSpeed-currentVelocity.Length())/(targetSpeed*2); //*2
                                SetEntityVelocity(vehicle.Handle,currentVelocity.X*modifier,currentVelocity.Y*modifier,currentVelocity.Z); //1.35f
                            }else{
                                SetEntityVelocity(vehicle.Handle,currentVelocity.X,currentVelocity.Y,currentVelocity.Z);
                            }
                               
                        }else{
                            Vector3 currentVelocity = item.Value;
                            FreezeEntityPosition(currentVehicle, false);
                            float currentSpeed = currentVelocity.Length();
                            
                            if(currentVelocity.Length()<targetSpeed){
                                float modifier = 1+(targetSpeed-currentVelocity.Length())/(targetSpeed*2); //*2
                                SetEntityVelocity(currentVehicle,currentVelocity.X*modifier,currentVelocity.Y*modifier,currentVelocity.Z); //1.35f
                            }else{
                                SetEntityVelocity(currentVehicle,currentVelocity.X,currentVelocity.Y,currentVelocity.Z);
                            }
                        }
                        
                        

                        
                        
                }
                vehiclesFrozen=false;
            }
            
        }

        public void StopDataCollection(int playerEntity){
            collectMode=false; 
            canStart=false;
            dashcamMode=false;
            //currentLocationIndex=0;

            FreezeEntityPosition(playerEntity, false);
            SetEntityInvincible(playerEntity, false);

            //Set's third person mode
            SetFollowPedCamViewMode(0);

            //Teleports player back to spawn location
            SetEntityCoords(playerEntity, -1257.721f, -1479.454f, 3.257412f, false, false, false, true);
        }

        public Task OnTick()
        {   
            //Updates the metadata without updating coordinate variables (for the onscreen bounding boxes)
            UpdateMetadata(false);

            int playerEntity = Game.PlayerPed.Handle; // set this as a global variable is probably a good idea
            
            if ((collectMode || dashcamMode) &&!vehiclesFrozen){ //added this to stop tick timer while cars are frozen
                tickCounter++;
            }else if (!vehiclesFrozen){
                tickCounter=0;
            }
            
            #region  If get info key is pressed f3
                if (Game.IsControlJustPressed(0, (Control)getInfoKey)){
                    Vector3 pCoords = GetEntityCoords(playerEntity, true);
                    float pHeading =    GetGameplayCamRelativeHeading();//GetEntityHeading(playerEntity);//GetGameplayCamRelativeHeading();
                 
                    Vector3 pRotation = GetEntityRotation(playerEntity,2);
                    float pPitch = GetGameplayCamRelativePitch();
                    float carSpeedTarget = 10f;
                    //Debug.WriteLine($"PITCH:{GetGameplayCamRelativePitch()}"); //, picsPerCondition, testMode
                    Debug.WriteLine($" new Location(new Vector3({pCoords.X}f, {pCoords.Y}f, {pCoords.Z}f), {pHeading}f, new Vector3({pRotation.X}f, {pRotation.Y}f, {pRotation.Z}f), {pPitch}f, {carSpeedTarget}f, {"picsPerCondition"}, testMode),");
                    //new Location(new Vector3(1769.527f, 3540.025f, 36.594f), 339.961f, new Vector3(0.000f, 0.000f, -20.039f),-1.670777f, 30);
                    
                }
                #endregion

            #region Change debug mode if key is pressed f5
                if (Game.IsControlJustPressed(0, (Control)debugKey)){
                    
                    debugMode=!debugMode;
                    
                }
                #endregion

            #region Change show box mode if key is pressed f6
                if (Game.IsControlJustPressed(0, (Control)showBoxKey)){
                    
                    showBoxMode=!showBoxMode;
                    Debug.Write($"SHOW BOX MODE: {showBoxMode}");
                    Debug.WriteLine($"PITCH:{GetGameplayCamRelativePitch()}");
                    
                }
                #endregion

            #region Change collect mode if key is pressed f1

            if (Game.IsControlJustPressed(0, (Control)collectKey)){
                    
                    if(!collectMode){ // If it is being changed to collect mode
                        
                        currentLocationIndex=0;
                        
                        Location currentLocation = locations[currentLocationIndex]; // repeated code block :(
                        targetSpeed = currentLocation.GetSpeed();
                        currentLocation.SetLocation(playerEntity);
                        collectMode=true;
                        canStart=false;
                        
                    }else{
                        StopDataCollection(playerEntity);
                    }
                }
                #endregion

            #region Change dashcam mode if key is pressed numpad 8

            if (Game.IsControlJustPressed(0, (Control)dashcamCollectKey)){
                    Debug.WriteLine($"helo");
                    if(!dashcamMode){ // If it is being changed to collect mode

                        
                        Ped playerPed = Game.PlayerPed;
                        Vector3 playerPos = playerPed.Position;
                        float closestDistance = -1f;
                        Vehicle closestVehicle = null;

                        if (!playerPed.IsInVehicle()){
                            foreach (Vehicle vehicle in World.GetAllVehicles()){
                                float distance = Vector3.Distance(playerPos, vehicle.Position);

                                if ((closestDistance == -1f || distance < closestDistance) && vehicle.Driver.Exists())
                                {
                                    closestDistance = distance;
                                    closestVehicle = vehicle;
                                }
                            }   

                            if (closestVehicle != null){
                                if (closestVehicle.Driver.Exists()){
                                    Debug.WriteLine($" Driver: {closestVehicle.Driver}");
                                    closestVehicle.Driver.Delete();
                                }
                                Debug.WriteLine($"Closest vehicle: {closestVehicle.Handle}, Distance: {closestDistance} IDK");
                                
                                new Ped(Game.PlayerPed.Handle).SetIntoVehicle(closestVehicle, VehicleSeat.Driver);
                                playerVehicle = closestVehicle;
                                SetFollowPedCamViewMode(4);

                                ClearPedTasks(Game.PlayerPed.Handle);

                                var veh = Game.PlayerPed.CurrentVehicle;
                                var model = (uint)veh.Model.Hash;

                                SetDriverAbility(Game.PlayerPed.Handle, 1f);
                                SetDriverAggressiveness(Game.PlayerPed.Handle, 0f);

                                TaskVehicleDriveWander(Game.PlayerPed.Handle, veh.Handle, GetVehicleModelMaxSpeed(model), 443);
                                ticksBetweenPics = ticksBetweenPicsDashcam;
                                playerVehicle.IsVisible = false;
                                SetEntityInvincible(playerVehicle.Handle, true);
                                SetEntityVisible(Game.PlayerPed.Handle, false, false);
                                targetSpeed = 1f;
                                tickCounter=0;
                                canStart = false;
                                dashcamMode = true;
                                
                            }
                        }
                    
                    }else{
                        dashcamMode = false;
                        ticksBetweenPics = ticksBetweenPicsBackup;
                        SetCamViewModeForContext(0, 0);
                        SetCamViewModeForContext(1, 0);
                        SetCamViewModeForContext(2, 0);
                        SetCamViewModeForContext(3, 0);
                        SetCamViewModeForContext(4, 0);
                        SetCamViewModeForContext(5, 0);
                        SetCamViewModeForContext(6, 0);
                        SetCamViewModeForContext(7, 0);
                        SetFollowPedCamViewMode(0);
                        playerVehicle.IsVisible = true;
                        SetEntityVisible(Game.PlayerPed.Handle, true, false);
                        StopDataCollection(playerEntity);
                    }
                }
                #endregion


            //if (dashcamMode){
            //    SetGameplayCamRelativePitch(0f, 1f);
            //    SetGameplayCamRelativeHeading(0f);
            //}

            /*if(dashcamMode){
                SetGameplayCamRelativePitch(0f, 1f);
                SetGameplayCamRelativeHeading(0f);
            }*/

            if(dashcamMode&&vehiclesFrozen&&!takingData){
                if(!canStart){ // if not ready to start
                    if (tickCounter<30){ //1500 
                            tickCounter+=1;
                            /*if (tickCounter % 10 == 0) {
                                UpdateMetadata(true);
                            }*/
                            
                            //SetGameplayCamRelativePitch(0f, 1f);
                            //SetGameplayCamRelativeHeading(0f);
                    }else{ // next tick it will be ready 
                        canStart=true;
                        tickCounter = 0;
                    }
                }else if(!takingData){
                    
                    takingData = true;
                    //canStart=false;
                    //Updates the metadata and updates coordinate variables
                    UpdateMetadata(true);
                    //Triggers the event to save the screenshot
                    //Debug.WriteLine($"Taking screenshot now: {takingData} {vehiclesFrozen} {tickCounter}");
                    TriggerServerEvent("saveImg", saveDir, currentID); //See ../Server/ServerSaveScreenshot.lua
                    
                    Vector3 playerPosition = Game.PlayerPed.Position;
                    // Add a blip to the map at the player's current position
                    int blipHandle = AddBlipForCoord(playerPosition.X, playerPosition.Y, playerPosition.Z);
                    // Customize the blip if needed
                    SetBlipSprite(blipHandle, 1); // Set blip sprite to standard waypoint
                    SetBlipColour(blipHandle, 5); // Set blip color to yellow
                    SetBlipScale(blipHandle, 1.0f); // Set blip scale
                    
                    canStart=false;
                    canEnd=true;
                    
                    
                    
                    //canStart=false;

                }
                

            }

            if(canEnd){
                if(!canStart){ // if not ready to start
                    if (tickCounter<10){ //1500 
                        tickCounter+=1;
                            //SetGameplayCamRelativePitch(0f, 1f);
                            //SetGameplayCamRelativeHeading(0f);
                    }else{ // next tick it will be ready 
                        canStart=true;
                        tickCounter = 0;
                    }
                }else{
                    canEnd=false;
                    FreezeVehicles(false);
                    takingData=false;
                }
                
            }

            if ((dashcamMode && tickCounter>=ticksBetweenPics) && !vehiclesFrozen &&!takingData){
                SetCamViewModeForContext(0, 4);
                SetCamViewModeForContext(1, 4);
                SetCamViewModeForContext(2, 4);
                SetCamViewModeForContext(3, 4);
                SetCamViewModeForContext(4, 4);
                SetCamViewModeForContext(5, 4);
                
                SetCamViewModeForContext(6, 4);
                SetCamViewModeForContext(7, 4);
                if(!canStart){ // if not ready to start
                    if (tickCounter<300){ //1500 
                        tickCounter+=1;
                        SetFollowPedCamViewMode(4);
                    }else{ // next tick it will be ready 
                        SetFollowPedCamViewMode(4);
                        canStart= true;
                        
                    }
                }else{
                    Vector3 velocity = Game.PlayerPed.Velocity;

                    // Calculate the speed (magnitude of the velocity vector)
                    float speed = velocity.Length();

                    if (speed>1){
                        ticksBetweenPics = ticksBetweenPicsBackup;
                    }else{
                        ticksBetweenPics = 250;
                        //Debug.WriteLine($"Stopped: {ticksBetweenPics} frames between screenshots");
                    }
                    

                    FreezeVehicles(true);
                    SetFollowPedCamViewMode(4);
                    //ClearPedTasks(Game.PlayerPed.Handle);
                    

                    // Set the camera's position and rotation to match the player's position and rotation
                    //SetGameplayCamRelativePitch(0f, 1f);
                    //SetGameplayCamRelativeHeading(0f);
                    //hmm2

                    if(vehiclesOnScreen){
                        canStart=false;
                       
                    }else{
                        
                        FreezeVehicles(false);
                        
                    }

                    ClearPedTasks(Game.PlayerPed.Handle);
                    var veh = Game.PlayerPed.CurrentVehicle;
                    var model = (uint)veh.Model.Hash;

                    SetDriverAbility(Game.PlayerPed.Handle, 1f);
                    SetDriverAggressiveness(Game.PlayerPed.Handle, 0f);

                    TaskVehicleDriveWander(Game.PlayerPed.Handle, veh.Handle, GetVehicleModelMaxSpeed(model), 443);
                   
                    
                    tickCounter=0;
                }
                
            }


            if (collectMode && tickCounter>=ticksBetweenPics&&!takingData){ // ready to take an image 
                //Debug.WriteLine($"{tickCounter}");
                if(!canStart){ // if not ready to start
                    if (tickCounter<500){ //1500 
                        tickCounter+=1;
                    }else{ // next tick it will be ready 
                        canStart= true;
                    }
                }else if(vehiclesOnScreen){ // if all parameters for taking a screenshot and saving metadata are true and there is a vehicle on the screen

                    if(!locations[currentLocationIndex].getShouldContinue()){ // if enough picsd have been taken from this location
                        
                        if(currentLocationIndex!=(locations.Length)-1){  // if there is another location to go 
                            collectMode= false;
                            canStart=false;
                            currentLocationIndex+=1;

                            Location currentLocation = locations[currentLocationIndex]; // repeated code block :(
                            targetSpeed = currentLocation.GetSpeed();
                            currentLocation.SetLocation(playerEntity);
                            collectMode=true;

                        }else{
                            StopDataCollection(playerEntity);
                        }    
                    }else{
                        //Resets timer for taking screenshots
                        tickCounter=0;

                        Location currentLocation = locations[currentLocationIndex];
                    
                        //Set Condition (time and weather)
                        currentLocation.SetCondition();
                        //Freezes all nearby vehicles
                        FreezeVehicles(true);
                        

                        if(vehiclesOnScreen){
                            //Updates the metadata and updates coordinate variables
                            takingData = true;
                            UpdateMetadata(true);
                            // Set the camera's position and rotation to match the player's position and rotation
                            //SetGameplayCamRelativePitch(0f, 1f);
                            //SetGameplayCamRelativeHeading(0f);
                            //hmm2
                            //Triggers the event to save the screenshot
                            TriggerServerEvent("saveImg", saveDir, currentID); //See ../Server/ServerSaveScreenshot.lua
                            canStart=false;
                            canEnd=true;
                            //FreezeVehicles(false);
                        }else{
                            FreezeVehicles(false);
                        }
                    }


                    
                    
                }
            
                
            }

        return Task.FromResult(0);
        
        }
        private void UpdateMetadata(bool save){

            playerPos = Game.PlayerPed.Position;
            //vehicles = World.GetAllVehicles().Where(e => e.IsOnScreen && e.Position.DistanceToSquared(playerPos) < entityRange && HasEntityClearLosToEntity(PlayerPedId(), e.Handle, 17)).ToList(); 
            //vehicles = World.GetAllVehicles().Where(e => e.IsOnScreen && e.Position.DistanceToSquared(playerPos) < entityRange && HasEntityClearLosToEntity(PlayerPedId(), e.Handle, 17)).ToList(); 
            vehicles = World.GetAllVehicles().Where(e => e.IsOnScreen ).ToList(); 
            

            vehiclesOnScreen = false;

            //if(vehicles.Count>0){
            //    vehiclesOnScreen = true;
            //}else{
           //     vehiclesOnScreen = false;
            //}

            List<Vehicle> every_vehicle = new List<Vehicle>();
            every_vehicle = (World.GetAllVehicles()).ToList(); 
            int vehicleCount = every_vehicle.Count;

            if(save){
                metadataList = new List<string>(){};
            }
            
            foreach (Vehicle v in vehicles)
            {
                
                if(debugMode){
                    //List<Vehicle> every_vehicle = new List<Vehicle>();
                    //every_vehicle = (World.GetAllVehicles()).ToList(); 
                    //int vehicleCount = every_vehicle.Count;
                    //Debug.WriteLine($"Total number of vehicles: {vehicleCount}  out of {vehicles.Count}");
                    DrawEntityBoundingBox(v, 250, 150, 0, 100);
                }

                List<int> pointCoordsX = new List<int>(){}; // list of all the x coords of all the things
                List<int> pointCoordsY = new List<int>(){};
                
                //List<int>[] visible = new List<int>[8];
                //List<int>[] bbX3D = new List<int>[8];
                //List<int>[] bbY3D = new List<int>[8];

                List<float> keypointsX = new List<float>(){};
                List<float> keypointsY = new List<float>(){};
                List<int> visible = new List<int>(){};

                
                /*for (int i = 0; i < visible.Length; i++)
                {
                    visible[i] = new List<int>();
                }
                for (int i = 0; i < bbX3D.Length; i++)
                {
                    bbX3D[i] = new List<int>();
                }
                for (int i = 0; i < visible.Length; i++)
                {
                    bbY3D[i] = new List<int>();
                }*/

                //float xVal =0f;
                //float yVal=0f;

                int xScreen=0;
                int yScreen=0;

                var vehicleBoxes = GetEntityBoundingBox(v.Handle);
                

                //GetScreenCoordFromWorldCoord(v.Position.X, v.Position.Y, v.Position.Z, ref xVal,ref yVal);
                GetActiveScreenResolution(ref xScreen, ref yScreen);


                int counter = 0;
                foreach(Vector3 vehicleBox in vehicleBoxes){

                    float xVal =0f;
                    float yVal=0f;

                    GetScreenCoordFromWorldCoord(vehicleBox.X, vehicleBox.Y, vehicleBox.Z, ref xVal,ref yVal);
                    
                    int currentXCoord = (int)(xVal*xScreen);
                    int currentYCoord = (int)(yVal*yScreen);  
                    

                    Vector3 destination = vehicleBox;
                    bool hit = false;
                    var coords = Vector3.Zero;
                    var normal = Vector3.Zero;
                    int target = 0;
                    int visibility;

                    Vector3 cameraCoord = GetGameplayCamCoord();
                    var idk = StartExpensiveSynchronousShapeTestLosProbe(cameraCoord.X, cameraCoord.Y, cameraCoord.Z, destination.X, destination.Y, destination.Z, -1, PlayerPedId(), 0);
                    GetShapeTestResult(idk, ref hit, ref coords, ref normal, ref target);

                    if(hit){
                        visibility = 1;
                    }else{
                        visibility = 2;
                    }

                    visible.Add(visibility);
                    keypointsX.Add(xVal);
                    keypointsY.Add(yVal);


                    
                    if(debugMode){
                        

                        SetDrawOrigin(vehicleBox.X, vehicleBox.Y, vehicleBox.Z, 0);
                        //DrawTextOnScreen($"{currentXCoord},{currentYCoord} {hit} {counter}", 0f, 0f, 0.3f, Alignment.Center, 0); //shows text on screen
                        ClearDrawOrigin();
                    }
                    
                    pointCoordsX.Add(currentXCoord);
                    pointCoordsY.Add(currentYCoord);
                    counter++;
                }

                
                int minX = pointCoordsX.Min();
                int minY = pointCoordsY.Min();

                int maxX = pointCoordsX.Max();
                int maxY = pointCoordsY.Max();

                
                /*if(debugMode){
                    SetDrawOrigin(v.Position.X, v.Position.Y, v.Position.Z+1.5f, 0);
                    //DrawTextOnScreen($"{v.DisplayName}\n{v.ClassLocalizedName} Total number of vehicles: {vehicleCount}  out of {vehicles.Count}", 0f, 0f, 0.3f, Alignment.Center, 0); // 
                    ClearDrawOrigin();
                }*/


                // Calculate the center point of the rectangle
                float centerX = (float)(minX + maxX) / 2.0f;
                float centerY = (float)(minY + maxY) / 2.0f;

                // Calculate the width and height of the rectangle
                float width = (float)(maxX - minX);
                float height = (float)(maxY - minY);

                // Convert the pixel coordinates to relative screen coordinates
                float relativeX = centerX / xScreen;
                float relativeY = centerY / yScreen;
                float relativeWidth = width / xScreen;
                float relativeHeight = height / yScreen;



                Vector3 destination2 = v.Position;
                bool hit2 = false;
                var coords2 = Vector3.Zero;
                var normal2 = Vector3.Zero;
                int target2 = 0;

                Vector3 cameraCoord2 = GetGameplayCamCoord();
                var idk2 = StartExpensiveSynchronousShapeTestLosProbe(cameraCoord2.X, cameraCoord2.Y, cameraCoord2.Z, destination2.X, destination2.Y, destination2.Z, -1, PlayerPedId(), 0);

                

                GetShapeTestResult(idk2, ref hit2, ref coords2, ref normal2, ref target2);
                
                if(hit2&&target2==v.Handle){
                    hit2=false;
                }

                if(debugMode){
                    SetDrawOrigin(v.Position.X, v.Position.Y, v.Position.Z, 0);
                    DrawTextOnScreen($"{hit2} by {target2} {v.Handle}", 0f, 0f, 0.3f, Alignment.Center, 0); // 
                    ClearDrawOrigin();
                }

                if ((minX >= 0 && minY >= 0 && maxX < xScreen && maxY < yScreen) && v.Position.DistanceToSquared(playerPos) < entityRange && HasEntityClearLosToEntity(PlayerPedId(), v.Handle, 17)&&!hit2){ //If the full bounding box is on the screen
                    vehiclesOnScreen = true;
                    if (playerVehicle!=null){
                        if (v.Handle!=playerVehicle.Handle){
                            v.IsVisible = true;
                        }
                    }else{
                            v.IsVisible = true;
                        }
                    
                    
                    if(showBoxMode){
                        DrawRect(relativeX, relativeY, relativeWidth, relativeHeight, 100, 255, 255, 150);
                    }
                    
                    if(save){
                        // Set the camera's position and rotation to match the player's position and rotation
                        //DrawRect(relativeX, relativeY, relativeWidth, relativeHeight, 255, 0, 0, 150);
                        metadataList.Add($"{v.ClassDisplayName.Split('_').Last()} {relativeX} {relativeY} {relativeWidth} {relativeHeight} {keypointsX[0]} {keypointsY[0]} {visible[0]} {keypointsX[1]} {keypointsY[1]} {visible[1]} {keypointsX[2]} {keypointsY[2]} {visible[2]} {keypointsX[3]} {keypointsY[3]} {visible[3]} {keypointsX[4]} {keypointsY[4]} {visible[4]} {keypointsX[5]} {keypointsY[5]} {visible[5]} {keypointsX[6]} {keypointsY[6]} {visible[6]} {keypointsX[7]} {keypointsY[7]} {visible[7]}\n"); //TODO: Fix this issue with the number
                    } //
                }else{
                    if(vehiclesFrozen && save){
                         int currentVeh = v.Handle;
                         if (playerVehicle!=null){
                              if (currentVeh!=playerVehicle.Handle){
                                //Debug.WriteLine($"Vehicle deleted: {currentVeh} {playerVehicle}");
                                //DeleteEntity(ref currentVeh);
                                v.IsVisible = false;
                            }  
                         }else{
                             //Debug.WriteLine($"Vehicle deleted: {currentVeh}");
                            //DeleteEntity(ref currentVeh);
                            v.IsVisible = false;
                         }
                        
                         
                        
                    }
                    
                }
            }
            if(save){
                 
                        //Debug.WriteLine("UPDATED METADATA");
                }
            }
        private void SaveMetadata(){

            //FreezeVehicles(false);
            //Debug.Write("SAVING METADATA");
            var metadataString =String.Join("",metadataList);
            //UpdateMetadata(true); //hmmm remove
            TriggerServerEvent("saveData", saveDir, currentID, metadataString);
            currentID++;
            //FreezeVehicles(false);
            
        }
        private void ChangeLocation(int locationIndex, int player){
            Location currentLocation = locations[locationIndex]; // repeated code block :(
            targetSpeed = currentLocation.GetSpeed();
            currentLocation.SetLocation(player);
            collectMode=true;
        }
    }

    class Location{

        Vector3 coords;
        float heading;
        Vector3 rotation;
        float pitch;
        float speed;
        int imageCounter = 0;
        int currentTime = 0;
        int currentWeather = 0;
        int targetImageNum;
        private int[] times = {12,18,0,6};
        string[] weathers = {"CLEAR", "RAIN", "FOGGY"};

       
        private bool shouldContinue = true;

        public Location(Vector3 playerCoords, float cameraHeading, Vector3 cameraRotation, float cameraPitch, float targetSpeed, int imageNum=1, bool simple=false){//150
            coords = playerCoords;
            heading = cameraHeading;
            rotation = cameraRotation;
            pitch = cameraPitch;
            speed = targetSpeed;
            targetImageNum = imageNum;

            if (simple){
                times = new int[]{12};
                weathers = new string[]{"CLEAR"};
            }else{
                targetImageNum = targetImageNum;//(imageNum)*(weathers.Length)*(times.Length);
            }


        }

        public void SetLocation(int player){
             //Set's players coords and camera position
            

            currentTime = 0;
            currentWeather = 0;
            imageCounter=0;
            shouldContinue = true;

            NetworkOverrideClockTime(times[currentTime], 00, 00);
            //SetOverrideWeather(weathers[currentWeather]);
            SetWeatherTypeNowPersist(weathers[currentWeather]);

            SetEntityCoords(player, coords.X, coords.Y, coords.Z, false, false, false, true);
            SetGameplayCamRelativeHeading(heading); 
            SetEntityRotation(player, rotation.X, rotation.Y, rotation.Z, 0, true);
            SetGameplayCamRelativePitch(pitch, 1f);
            

            //Set's first person mode
            SetFollowPedCamViewMode(4);

            FreezeEntityPosition(player, true);
            SetEntityInvincible(player, true);

            Vector3 playerPosition = Game.PlayerPed.Position;
            // Add a blip to the map at the player's current position
            int blipHandle = AddBlipForCoord(playerPosition.X, playerPosition.Y, playerPosition.Z);
            // Customize the blip if needed
            SetBlipSprite(blipHandle, 1); // Set blip sprite to standard waypoint
            SetBlipColour(blipHandle, 5); // Set blip color to yellow
            SetBlipScale(blipHandle, 1.0f); // Set blip scale
                        
        }

        public void SetCondition(){// alwasys done before an image is taken 
            
            if(imageCounter>=targetImageNum){//if condition should be changed 
                if(currentTime!=(times.Length)-1){
                    //increase the time by one and reset image counter
                    currentTime+=1;
                    SetTime();
                    imageCounter=0;
                }else if(currentWeather!=(weathers.Length)-1){
                    //reset the current time to 0
                    currentTime=0;
                    SetTime();
                    // increase weather by one and reset image counter
                    currentWeather+=1;
                    //SetOverrideWeather(weathers[currentWeather]);
                    SetWeatherTypeNowPersist(weathers[currentWeather]);
                    imageCounter=0;
                }else{
                    shouldContinue=false;
                }
            //attempt to change time
            //if time can't be changed then attempt to change weather
            // if weather can't be changed then change the value of should continue 

            }
            
            imageCounter++; //Should this be in an else? Probably not


            // if the condition needs to be changed, change it :)
        }

        public void SetTime(){
            NetworkOverrideClockTime(times[currentTime], 00, 00);
        }

        public float GetSpeed(){
            return speed;
        }

        public bool getShouldContinue(){
            return shouldContinue;
        }

    }
}
