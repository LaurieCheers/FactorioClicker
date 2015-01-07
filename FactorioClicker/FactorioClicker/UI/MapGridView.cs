using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FactorioClicker.Simulation;
using Microsoft.Xna.Framework.Graphics;
using FactorioClicker.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using System.Reflection;

namespace FactorioClicker.UI
{
    public enum TerrainType
    {
        Space = 0,
        Land = 1,
        Toxic = 2,
        Sea = 4,
        Orbit = 8,
        Asteroid = 16,
    }

    public abstract class SpaceGridTool
    {
        public String displayName;
        public LayeredImage buttonIcon;
        public MapGridView spaceGrid;

        public SpaceGridTool(JSONTable template)
        {
            displayName = template.getString("name", null);
        }

        public void SelectTool()
        {
            spaceGrid.SelectTool(this);
        }

        public static SpaceGridTool newFromTemplate(MapGridView spaceView, JSONTable template, ContentManager Content)
        {
            String type = template.getString("type");
            Assembly asm = typeof(SpaceGridTool).Assembly;
            Type t = asm.GetType("FactorioClicker.UI."+type);
            if (t != null)
            {
                SpaceGridTool result = (SpaceGridTool)Activator.CreateInstance(t, new object[] { spaceView, template, Content });
                return result;
            }
            else
            {
                return null;
            }
        }

        public abstract void Draw(SpriteBatch spriteBatch);
        public abstract bool HandleInput(InputState inputState, JSCNContext context);
    }

    class SpaceGridTool_Buildings: SpaceGridTool
    {
        String tooltipText;
        GridItem mouseOverItem;
        GridPoint mouseOverPoint;
        LayeredImage highlightImage;
        LayeredImage blockedImage;
        GridItem_Settlement stationType;

        public SpaceGridTool_Buildings(MapGridView aSpaceGrid, JSONTable template, ContentManager Content): base(template)
        {
            spaceGrid = aSpaceGrid;
            highlightImage = new LayeredImage(template.getJSON("highlight"), Content);
            blockedImage = new LayeredImage(template.getJSON("blocked"), Content);

            string stationTypeName = template.getString("stationType");
            stationType = Game1.instance.settlementTypes[stationTypeName];
            
            buttonIcon = stationType.itemType.image;
            displayName = stationType.itemType.displayName;
        }

        public override bool HandleInput(InputState inputState, JSCNContext context)
        {
            mouseOverPoint = spaceGrid.screenToGridPos(inputState.MousePos);

            GridItem mouseOverPlanet = spaceGrid.planetGrid.ItemAtGridPos(mouseOverPoint);
            mouseOverItem = mouseOverPlanet;

            GridItem_Settlement mouseOverStation = (GridItem_Settlement)spaceGrid.grid.ItemAtGridPos(mouseOverPoint);
            GridItem orbitingPlanet = null;

            if (mouseOverPlanet != null)
            {
                tooltipText = mouseOverPlanet.itemType.name;
            }
            else
            {
                tooltipText = "";

                foreach (GridPoint adjacent in mouseOverPoint.Adjacent4)
                {
                    GridItem adjacentItem = spaceGrid.planetGrid.ItemAtGridPos(adjacent);
                    if (adjacentItem != null)
                    {
                        tooltipText = adjacentItem.itemType.name + " Orbit";
                        orbitingPlanet = adjacentItem;
                        break;
                    }
                }
            }

            if (inputState.WasMouseLeftJustPressed())
            {
                if (mouseOverStation == null)
                {
                    // create a station
                    spaceGrid.AddStation(stationType, mouseOverPoint);
                    spaceGrid.SelectTool(null);
                }
                return true;
            }
            else if (inputState.WasMouseRightJustPressed())
            {
                spaceGrid.SelectTool(null);
                return true;
            }
            return false;
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
/*            if (tooltipText != "" && tooltipText != null)
            {
                Vector2 tooltipSize = Game1.font.MeasureString(tooltipText);

                Rectangle itemRect;
                if (mouseOverItem != null)
                {
                    itemRect = spaceGrid.gridToScreenRect(mouseOverItem.gridPosition, mouseOverItem.gridSize);
                }
                else
                {
                    itemRect = spaceGrid.gridToScreenRect(mouseOverPoint, GridSize.Unit);
                }

                Rectangle tooltipRect = new Rectangle((int)(itemRect.Center.X - tooltipSize.X / 2), (int)(itemRect.Top - tooltipSize.Y - 5), (int)(tooltipSize.X), (int)(tooltipSize.Y));
                tooltipImage.Draw(spriteBatch, tooltipRect);
                spriteBatch.DrawString(Game1.font, tooltipText, tooltipRect.TopLeft(), Color.White);
            }*/

            if( stationType.CanBuild(spaceGrid.getTerrainType(mouseOverPoint)) &&
                spaceGrid.grid.ItemAtGridPos(mouseOverPoint) == null)
            {
                highlightImage.Draw(spriteBatch, spaceGrid.gridToScreenRect(mouseOverPoint, GridSize.Unit));
            }
            else
            {
                blockedImage.Draw(spriteBatch, spaceGrid.gridToScreenRect(mouseOverPoint, GridSize.Unit));
            }
        }
    }

    class SpaceGridTool_View : SpaceGridTool
    {
        GridItem_Settlement mouseOverStation;
        GridItem mouseOverPlanet;
        GridPoint mouseOverPoint;
        LayeredImage highlightImage;

        public SpaceGridTool_View(MapGridView aSpaceGrid, JSONTable template, ContentManager Content): base(template)
        {
            spaceGrid = aSpaceGrid;
            highlightImage = new LayeredImage(template.getJSON("highlight"), Content);
            buttonIcon = new LayeredImage(template.getJSON("icon"), Content);
        }

        public override bool HandleInput(InputState inputState, JSCNContext context)
        {
            mouseOverPoint = spaceGrid.screenToGridPos(inputState.MousePos);

            mouseOverStation = (GridItem_Settlement)spaceGrid.grid.ItemAtGridPos(mouseOverPoint);

            if (mouseOverStation != null)
            {
                mouseOverPlanet = null;
            }
            else
            {
                mouseOverPlanet = spaceGrid.planetGrid.ItemAtGridPos(mouseOverPoint);
            }

            if (inputState.WasMouseLeftJustPressed())
            {
                if (mouseOverStation != null)
                {
                    // view a station
                    Game1.instance.OpenGridEditor(mouseOverStation);
                }
                return true;
            }
            return false;
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            GridItem item = null;

            if (mouseOverStation != null)
            {
                item = mouseOverStation;
            }
            else if (mouseOverPlanet != null)
            {
                item = mouseOverPlanet;
            }

            if (item != null)
            {
                String tooltipText = item.itemType.displayName;
                Rectangle itemRect = spaceGrid.gridToScreenRect(item.gridPosition, item.gridSize);
                Game1.instance.DrawTooltip(spriteBatch, itemRect, UIAnchorSide.TOP, tooltipText);
            }

            if( mouseOverStation != null )
            {
                highlightImage.Draw(spriteBatch, spaceGrid.gridToScreenRect(mouseOverPoint, GridSize.Unit));
            }
        }
    }

    public class MapGridView: GridView, JSCNContext
    {
        public Grid planetGrid;
        List<GridItem_Settlement> settlements;
        SpaceGridTool selectedTool;
        SpaceGridTool defaultTool;
        TerrainType[,] terrainTypes;

        public float time = 0;
        public float timeScale = 1;
        float nextUpdateTime = 0;
        public bool paused = true;

        public Dictionary<string, SpaceGridTool> toolPalette;
        public WorkerManager workerManager = new WorkerManager();

        public MapGridView(JSONTable template, ContentManager Content):
            base(null, template, Content)
        {
            GridSize gridSize = new GridSize(template.getArray("gridSize"));
            grid = new Grid(gridSize, new ResourceGrid(gridSize, null, GridPoint.Zero));
            planetGrid = new Grid(gridSize, null);
            settlements = new List<GridItem_Settlement>();
            toolPalette = new Dictionary<string, SpaceGridTool>();

            JSONTable resourceTemplate = template.getJSON("resources");
            foreach (String key in resourceTemplate.Keys)
            {
                ResourceType resourceType = Game1.instance.resourceTypes[key];
                JSONArray positionsList = resourceTemplate.getArray(key);
                for(int Idx = 0; Idx < positionsList.Length; ++Idx)
                {
                    grid.resources.TryProduceResource(ResourceOutputMode.NORMAL, new GridPoint(positionsList.getArray(Idx)), resourceType, resourceType.maxAmount, true);
                }
            }

            JSONTable toolsTemplate = template.getJSON("tools");
            foreach(string toolName in toolsTemplate.Keys)
            {
                SpaceGridTool tool = SpaceGridTool.newFromTemplate(this, toolsTemplate.getJSON(toolName), Content);
                toolPalette[toolName] = tool;

                if (selectedTool == null)
                {
                    selectedTool = tool;
                }
                if (defaultTool == null)
                {
                    defaultTool = tool;
                }
            }

            terrainTypes = new TerrainType[gridSize.Width, gridSize.Height];


            JSONArray planetsTemplate = template.getArray("planets");
            for (int Idx = 0; Idx < planetsTemplate.Length; ++Idx)
            {
                JSONTable thisPlanetTemplate = planetsTemplate.getJSON(Idx);
                GridItem planet = new GridItem(thisPlanetTemplate, Content);
                planetGrid.Add(planet);
                
                TerrainType terrainType = thisPlanetTemplate.getString("terrainType").toTerrainType();

                for(int x = 0; x < planet.gridSize.Width; x++ )
                {
                    for(int y = 0; y < planet.gridSize.Height; y++)
                    {
                        terrainTypes[x + planet.gridPosition.X, y + planet.gridPosition.Y] = terrainType;
                    }
                }

                for (int orbitN = 0; orbitN < planet.gridSize.Width; orbitN++)
                {
                    terrainTypes[orbitN + planet.gridPosition.X, planet.gridPosition.Y - 1] = TerrainType.Orbit;
                    terrainTypes[planet.gridPosition.X - 1, orbitN + planet.gridPosition.Y] = TerrainType.Orbit;
                    terrainTypes[orbitN + planet.gridPosition.X, planet.gridPosition.Y + planet.gridSize.Height] = TerrainType.Orbit;
                    terrainTypes[planet.gridPosition.X + planet.gridSize.Width, orbitN + planet.gridPosition.Y] = TerrainType.Orbit;
                }

            }
        }

        public TerrainType getTerrainType(GridPoint position)
        {
            if (grid.IsOutOfBounds(position))
                return TerrainType.Space;
            else
                return terrainTypes[position.X, position.Y];
        }

        public override bool HandleInput(InputState inputState, JSCNContext context)
        {
            if (selectedTool != null)
            {
                return selectedTool.HandleInput(inputState, context);
            }
            else
            {
                return false;
            }
        }

        public void Update()
        {
            if (!paused)
            {
                if (time >= ticksPerYear)
                {
                    time = 0;
                    paused = true;
                    Game1.instance.OpenFestivalScreen();
                }
                else
                {
                    time += timeScale;
                    nextUpdateTime += timeScale;

                    while (nextUpdateTime >= 1)
                    {
                        nextUpdateTime--;

                        foreach (GridItem_Settlement station in settlements)
                        {
                            station.Update();
                            workerManager.UpdateItem(station);
                        }

                        workerManager.Update(grid);
                    }
                }
            }
        }

        public void OnYearEnd()
        {
            ResetYear();
        }

        public void ResetYear()
        {
            time = 0;
            foreach (GridItem_Settlement settlement in settlements)
            {
                settlement.ResetYear();
            }

        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            DrawGrid(planetGrid, spriteBatch);
            DrawGrid(grid, spriteBatch);

            workerManager.Draw(spriteBatch, this);

            if (selectedTool != null)
            {
                selectedTool.Draw(spriteBatch);
            }
        }

        public GridItem_Settlement AddStation(GridItem_Settlement stationType, GridPoint position)
        {
            GridItem_Settlement newStation = stationType.Clone_SpaceStation();
            settlements.Add(newStation);
            newStation.CreateGrid(grid.resources, position);
            newStation.gridPosition = position;
            grid.Add(newStation);
            return newStation;
        }

        public override System.Object getProperty(String aName)
        {
            if (aName == "setTimescale")
            {
                return new JSCNFunctionValue(FN_setTimescale);
            }
            else if (aName == "restartYear")
            {
                return new JSCNFunctionValue(FN_restartYear);
            }
            else if (aName == "closeFestivalScreen")
            {
                return new JSCNFunctionValue(FN_closeFestivalScreen);
            }
            else if (aName == "timetext")
            {
                return this.timeText;
            }
            return null;
        }

        public override JSCNContext getElement(string name)
        {
            return null;
        }

        System.Object FN_setTimescale(JSONArray parameters)
        {
            paused = false;
            timeScale = parameters.getFloat(0);
            return null;
        }

        System.Object FN_closeFestivalScreen(JSONArray parameters)
        {
            Game1.instance.CloseFestivalScreen();
            return null;
        }

        System.Object FN_restartYear(JSONArray parameters)
        {
            ResetYear();
            timeScale = 0;
            return null;
        }

        static int[] monthLengths = new int[] { 31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31 };
        static string[] monthNames = new string[] { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December" };
        static int ticksPerDay = 10;
        static int ticksPerYear = 365 * ticksPerDay;

        public string timeText
        {
            get
            {
                float daysRemaining = time / ticksPerDay;

                int currentMonth = 0;
                while (daysRemaining > monthLengths[currentMonth])
                {
                    daysRemaining -= monthLengths[currentMonth];
                    currentMonth = (currentMonth + 1) % 12;
                }

                return monthNames[currentMonth] + " " + Convert.ToString((int)daysRemaining + 1);
            }
        }

        public void SelectTool(SpaceGridTool tool)
        {
            if (tool == null)
            {
                selectedTool = defaultTool;
            }
            else
            {
                selectedTool = tool;
            }
        }
    }
}
