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
        private int currentID = 0;
        public static int getInfoKey = 170; //f3
        public static int debugKey = 166; //f5
        public static int showBoxKey = 167; //f6
        public static int collectKey = 288; //f1
        public static int carOnlyCollectKey = 168; //f8
        internal static float entityRange = 10000f;//15000f;
        public float targetSpeed = 30f;
        private int picsFromLocation = 50;
        private int ticksBetweenPics = 60;

        
        string saveDir = @"D:\Dissertation\dataset\";

        private List<Vehicle> vehicles = new List<Vehicle>();
        private Vector3 playerPos;
        private string filePath;
        private List<string> metadataList;
        private int tickCounter = 0;
        private string metadataString = "";
        private bool saveMetadata = false;
        private bool vehiclesFrozen = false;
        private DateTime start;
        private List<Vector3> vehicleVelocities;
        private Dictionary<int, Vector3> carVelocityDict ;
        private bool canStart = false;
        private Location location1;
        private Location location2;
        private bool vehiclesOnScreen=false;
        //private List<Location> locations;
        private Location[] locations;
        private int currentLocationIndex = 0;

        private bool generateTestDataset=true;
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

            
            picsFromLocation = 10;
            locations = new Location[]{
                //new Location(new Vector3(-106.6908f, -519.0898f, 39.84289f), 0f, new Vector3(0f, 0f, -8.321015f), -30.22839f, 10f, picsFromLocation, true),
                new Location(new Vector3(-396.6956f, 258.3295f, 84.95533f), -0.08379275f, new Vector3(0f, 0f, -169.8985f), -9.746197f, 10f, picsFromLocation, true),
                new Location(new Vector3(-684.6088f, -1201.297f, 11.48662f), 6.830189E-06f, new Vector3(0f, 0f, 99.10529f), -4.075363f, 10f, picsFromLocation, true),
                new Location(new Vector3(-1099.225f, -1308.184f, 6.221095f), 1.366038E-05f, new Vector3(0f, 0f, 114.9308f), 0.3984259f, 10f, picsFromLocation, true),
                new Location(new Vector3(-996.4599f, -856.6437f, 14.02444f), -0.08833483f, new Vector3(0f, 0f, 69.45237f), -6.899021f, 10f, picsFromLocation, true),
                new Location(new Vector3(-1013.158f, -852.0976f, 17.15666f), 0f, new Vector3(0f, 0f, -29.30029f), -18.85762f, 10f, picsFromLocation, true),
                new Location(new Vector3(-924.1362f, -543.504f, 25.23962f), -8.537736E-07f, new Vector3(0f, 0f, -14.02249f), -19.26244f, 10f, picsFromLocation, true),
                new Location(new Vector3(-941.0885f, -554.3464f, 33.30359f), 6.830189E-06f, new Vector3(0f, 0f, 110.3936f), -39.78363f, 10f, picsFromLocation, true),
                new Location(new Vector3(-1099.231f, -673.6322f, 25.81387f), 1.366038E-05f, new Vector3(0f, 0f, 135.4087f), -13.31058f, 10f, picsFromLocation, true),
                new Location(new Vector3(-1098.779f, -729.7245f, 24.64202f), 0f, new Vector3(0f, 0f, 41.71575f), -26.91467f, 10f, picsFromLocation, true),
                new Location(new Vector3(-801.694f, -78.85551f, 61.41818f), -3.415094E-06f, new Vector3(0f, 0f, -48.00604f), -51.43717f, 10f, picsFromLocation, true),
                new Location(new Vector3(-781.9294f, 217.2976f, 77.67545f), 0f, new Vector3(0f, 0f, -100.372f), -24.58274f, 10f, picsFromLocation, true),
                new Location(new Vector3(241.6309f, 305.2818f, 106.5246f), 0f, new Vector3(0f, 0f, -16.41452f), -9.46713f, 10f, picsFromLocation, true),
            };
            
            TriggerServerEvent("generateDirs", $"{saveDir}\\images", $"{saveDir}\\labels");

            EventHandlers["updateMetadata"] += new Action<bool> (UpdateMetadata);
            EventHandlers["saveMetadata"] += new Action (SaveMetadata);
            Tick += OnTick;
        }


        private void FreezeVehicles(bool freezeMode){

            Vehicle[] allNearbyCars = World.GetAllVehicles();
            

            if(freezeMode){

                vehicleVelocities = new List<Vector3>{};
                carVelocityDict = new Dictionary<int, Vector3>();

                foreach (Vehicle v in allNearbyCars){
                    int currentVeh = v.Handle;


                    Vector3 currentVel = GetEntityVelocity(currentVeh);
                    if(currentVel==new Vector3(0,0,0) || currentVel.Length()<targetSpeed/4){ //Target speed /6   targetSpeed/4 7.5f
                        DeleteEntity(ref currentVeh);

                    }else{
                        carVelocityDict.Add(currentVeh, currentVel);
                        FreezeEntityPosition(currentVeh, true);
                    }
                    
                 }
                 
                vehiclesFrozen=true;
                
            }else{
                foreach(KeyValuePair<int, Vector3> item in carVelocityDict){
                        int currentVehicle = item.Key;
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
                vehiclesFrozen=false;
            }
        }

        public void StopDataCollection(int playerEntity){
            collectMode=false; 
            canStart=false;
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
            
            if (collectMode){
                tickCounter++;
            }else{
                tickCounter=0;
            }
            
            #region  If get info key is pressed f3
                if (Game.IsControlJustPressed(0, (Control)getInfoKey)){
                    Vector3 pCoords = GetEntityCoords(playerEntity, true);
                    float pHeading =    GetGameplayCamRelativeHeading();//GetEntityHeading(playerEntity);//GetGameplayCamRelativeHeading();
                 
                    Vector3 pRotation = GetEntityRotation(playerEntity,2);
                    float pPitch = GetGameplayCamRelativePitch();
                    float carSpeedTarget = 10f;
                    //Debug.WriteLine($"PITCH:{GetGameplayCamRelativePitch()}");
                    Debug.WriteLine($" new Location(new Vector3({pCoords.X}f, {pCoords.Y}f, {pCoords.Z}f), {pHeading}f, new Vector3({pRotation.X}f, {pRotation.Y}f, {pRotation.Z}f), {pPitch}f, {carSpeedTarget}f, {"picsFromLocation"}, true),");
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
                        
                    }else{
                        StopDataCollection(playerEntity);
                    }
                }
                #endregion

            if (collectMode && tickCounter>=ticksBetweenPics){ // ready to take an image 
                
                if(!canStart){ // if not ready to start
                    if (tickCounter<3000){ //1500 
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
                    
                    //Updates the metadata and updates coordinate variables
                    UpdateMetadata(true);
                    
                    //Triggers the event to save the screenshot
                    TriggerServerEvent("saveImg", saveDir, currentID); //See ../Server/ServerSaveScreenshot.lua
                }


                    
                    
                }
            
                
            }

        return Task.FromResult(0);
        
        }
        private void UpdateMetadata(bool save){

            playerPos = Game.PlayerPed.Position;
            //vehicles = World.GetAllVehicles().Where(e => e.IsOnScreen && e.Position.DistanceToSquared(playerPos) < entityRange && HasEntityClearLosToEntity(PlayerPedId(), e.Handle, 17)).ToList(); 
            vehicles = World.GetAllVehicles().Where(e => e.IsOnScreen && e.Position.DistanceToSquared(playerPos) < entityRange && HasEntityClearLosToEntity(PlayerPedId(), e.Handle, 17)).ToList(); 
            

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
                        DrawTextOnScreen($"{currentXCoord},{currentYCoord} {hit} {counter}", 0f, 0f, 0.3f, Alignment.Center, 0);
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

                
                if(debugMode){
                    SetDrawOrigin(v.Position.X, v.Position.Y, v.Position.Z+1.5f, 0);
                    DrawTextOnScreen($"{v.DisplayName}\n{v.ClassLocalizedName} Total number of vehicles: {vehicleCount}  out of {vehicles.Count}", 0f, 0f, 0.3f, Alignment.Center, 0); // 
                    ClearDrawOrigin();
                }


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

                
                if (minX >= 0 && minY >= 0 && maxX < xScreen && maxY < yScreen){ //If the full bounding box is on the screen
                    vehiclesOnScreen = true;
                    if(showBoxMode){
                        DrawRect(relativeX, relativeY, relativeWidth, relativeHeight, 100, 255, 255, 150);
                    }
                    
                    if(save){
                        metadataList.Add($"{v.ClassDisplayName.Split('_').Last()} {relativeX} {relativeY} {relativeWidth} {relativeHeight} {keypointsX[0]} {keypointsY[0]} {visible[0]} {keypointsX[1]} {keypointsY[1]} {visible[1]} {keypointsX[2]} {keypointsY[2]} {visible[2]} {keypointsX[3]} {keypointsY[3]} {visible[3]} {keypointsX[4]} {keypointsY[4]} {visible[4]} {keypointsX[5]} {keypointsY[5]} {visible[5]} {keypointsX[6]} {keypointsY[6]} {visible[6]} {keypointsX[7]} {keypointsY[7]} {visible[7]}\n"); //TODO: Fix this issue with the number
                    } //
                }else{
                    if(vehiclesFrozen && save){
                         int currentVeh = v.Handle;
                         Debug.WriteLine($"Vehicle deleted: {currentVeh}");
                        DeleteEntity(ref currentVeh);
                        
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
            TriggerServerEvent("saveData", saveDir, currentID, metadataString);
            currentID++;
            FreezeVehicles(false);
            
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
        private int[] times = {0,6,12,18};
        string[] weathers = {"CLEAR", "RAIN", "FOGGY"};

        string[] vehicles = {
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
                targetImageNum = (imageNum)*(weathers.Length)*(times.Length);
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
