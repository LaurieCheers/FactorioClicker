using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using FactorioClicker.UI;
using System.IO;
using FactorioClicker.Simulation;
using FactorioClicker.Graphics;

namespace FactorioClicker
{
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class Game1 : Microsoft.Xna.Framework.Game
    {
        public static Game1 instance;
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;
        public UIManager uiManager { get; private set; }
        LayeredImage tooltipImage;
        public LayeredImage powerSymbolImage;
        public LayeredImage busyLightImage;
        public ResearchManager researchManager;

        public int money;

        MapGridView spaceView;
        GridEditor_SpaceStation gridEditor;
        UIScreen gridEditorScreen;
        UIScreen festivalScreen;

        public Dictionary<String, GridItem_Building> buildingTypes;
        public Dictionary<String, GridItem_Settlement> settlementTypes;
        public Dictionary<String, ResourceType> resourceTypes;
        public Dictionary<String, FactoryWorkerType> workerTypes;

        public static SpriteFont font { get; private set; }

        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
            instance = this;
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            // TODO: Add your initialization logic here

            base.Initialize();
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);

            Rectangle screenSize = GraphicsDevice.Viewport.Bounds;

            font = Content.Load<SpriteFont>("SpriteFont1");

            uiManager = new UIManager();
            UIScreen mainScreen = new UIScreen();
            uiManager.PushScreen(mainScreen);

            FileStream fs = File.OpenRead("Content/machines.txt");
            StreamReader sr = new StreamReader(fs);

            String machinesTxt = sr.ReadToEnd();
            fs.Close();

            JSONTable gameTemplate = JSONTable.parse(machinesTxt);

            tooltipImage = new LayeredImage( gameTemplate.getJSON("tooltip"), Content);
            powerSymbolImage = new LayeredImage( gameTemplate.getJSON("powerSymbol"), Content);
            busyLightImage = new LayeredImage( gameTemplate.getJSON("busyLight"), Content);

            // NB: resources and worker types must be loaded before buildings
            JSONTable resourcesTemplate = gameTemplate.getJSON("resources");
            resourceTypes = new Dictionary<string, ResourceType>();
            foreach (String s in resourcesTemplate.Keys)
            {
                resourceTypes[s] = new ResourceType(resourcesTemplate.getJSON(s), Content);
            }

            JSONTable workersTemplate = gameTemplate.getJSON("workers");
            workerTypes = new Dictionary<string, FactoryWorkerType>();
            foreach (String s in workersTemplate.Keys)
            {
                workerTypes[s] = new FactoryWorkerType(workersTemplate.getJSON(s), Content);
            }

            // NB: settlements must be loaded before the spaceView
            JSONTable settlementsTemplate = gameTemplate.getJSON("settlements");
            settlementTypes = new Dictionary<string, GridItem_Settlement>();
            foreach (String s in settlementsTemplate.Keys)
            {
                settlementTypes[s] = new GridItem_Settlement(s, settlementsTemplate.getJSON(s), Content);
            }

            JSONTable buildingsTemplate = gameTemplate.getJSON("buildings");
            buildingTypes = new Dictionary<string, GridItem_Building>();
            foreach (String s in buildingsTemplate.Keys)
            {
                buildingTypes[s] = new GridItem_Building(s, buildingsTemplate.getJSON(s), Content);
            }

            festivalScreen = new UIScreen(gameTemplate.getJSON("festivalScreen"), Content);

            // GridEditor
            gridEditorScreen = new UIScreen(gameTemplate.getJSON("stationScreen"), Content);

            UIScreen spaceScreen = new UIScreen(gameTemplate.getJSON("spaceScreen"), Content);
            uiManager.PushScreen(spaceScreen);

            spaceView = (MapGridView)spaceScreen.getElement("spacegrid");
            gridEditor = (GridEditor_SpaceStation)gridEditorScreen.getElement("grideditor");

            researchManager = new ResearchManager(gameTemplate.getArray("research"), resourceTypes);

            foreach (JSONTable settlementTemplate in gameTemplate.getArray("prebuilt", JSONArray.empty).asJSONTables())
            {
                PrebuiltSettlementTemplate p = new PrebuiltSettlementTemplate(settlementTemplate);
                p.Build(spaceView);
            }

/*            UIButton targetButton = new UIButton("hello", new Rectangle(400, 400, 100, 30), Content);
            mainScreen.Add(targetButton);

            UIBubble buttonPanel = new UIBubble(targetButton, Content);
            mainScreen.Add(buttonPanel);

            for (int yPos = 0; yPos < 36; yPos += 35)
            {
                buttonPanel.Add(new UIButton(Content.Load<Texture2D>("well"), new Rectangle(0, yPos, 100, 30), Content));
            }

            resources.cells[1, 1].type = resourceTypes["coalbed"];
            resources.cells[1, 1].amount = 100;

            resources.cells[3, 5].type = resourceTypes["ironbed"];
            resources.cells[3, 5].amount = 100;*/
        }

        public void OpenGridEditor(GridItem_Settlement grid)
        {
            gridEditor.OpenStation(grid);
            uiManager.PushScreen(gridEditorScreen);
        }

        public void OpenFestivalScreen()
        {
            uiManager.PushScreen(festivalScreen);
        }

        public void CloseFestivalScreen()
        {
            uiManager.PopScreen();
            spaceView.OnYearEnd();
            researchManager.OnYearEnd();
        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// all content.
        /// </summary>
        protected override void UnloadContent()
        {
            // TODO: Unload any non ContentManager content here
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            // Allows the game to exit
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
                this.Exit();

            uiManager.Update();
            spaceView.Update();
            researchManager.Update();

            base.Update(gameTime);
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            // TODO: Add your drawing code here
            spriteBatch.Begin();

            uiManager.Draw(spriteBatch);

            Vector2 wheatPos = new Vector2(GraphicsDevice.Viewport.Width - 200, GraphicsDevice.Viewport.Height - 100);
            String wheatLabel = "Wheat produced:" + researchManager.GetProductionTracker(resourceTypes["wheat"]).yearTotal;
            spriteBatch.DrawString(font, wheatLabel, wheatPos + new Vector2(1, 1), Color.Black);
            spriteBatch.DrawString(font, wheatLabel, wheatPos, Color.Green);

            Vector2 moneyPos = new Vector2(GraphicsDevice.Viewport.Width - 100, GraphicsDevice.Viewport.Height - 50);
            String moneyLabel = "$"+money.ToString();
            spriteBatch.DrawString(font, moneyLabel, moneyPos + new Vector2(1, 1), Color.Black);
            spriteBatch.DrawString(font, moneyLabel, moneyPos, Color.Yellow);

            spriteBatch.End();

            base.Draw(gameTime);
        }

        public void DrawTooltip(SpriteBatch spriteBatch, Rectangle anchorRect, UIAnchorSide anchorSide, String tooltipText)
        {
            Vector2 tooltipSize = Game1.font.MeasureString(tooltipText);

            Rectangle tooltipRect = anchorRect.getAnchoredRect(anchorSide, tooltipSize);
            if (tooltipRect.X < 0)
                tooltipRect = new Rectangle(0, tooltipRect.Y, tooltipRect.Width, tooltipRect.Height);
            if (tooltipRect.Y < 0)
                tooltipRect = new Rectangle(tooltipRect.X, 0, tooltipRect.Width, tooltipRect.Height);
            tooltipImage.Draw(spriteBatch, tooltipRect);
            spriteBatch.DrawString(Game1.font, tooltipText, tooltipRect.TopLeft(), Color.White);
        }

        public void DebugLog(String text)
        {
            Console.WriteLine(text);
        }
    }
}
