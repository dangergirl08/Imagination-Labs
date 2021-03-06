#region File Description
//-----------------------------------------------------------------------------
// ScreenManager.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------
#endregion

#region Using Statements
using System;
using System.Diagnostics;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input.Touch;
using Microsoft.Kinect;
using Microsoft.Speech;
using GameStateManagement.GestureSelector;
using Microsoft.Speech.Recognition;
//using ShapeGame.Speech;


using GameStateManagement;
using GameStateManagement.DTWGestureRecognition;
#endregion

namespace GameStateManagement
{
    /// <summary>
    /// The screen manager is a component which manages one or more GameScreen
    /// instances. It maintains a stack of screens, calls their Update and Draw
    /// methods at the appropriate times, and automatically routes input to the
    /// topmost active screen.
    /// </summary>
    public class ScreenManager : DrawableGameComponent
    {
        #region Variables

        List<GameScreen> screens = new List<GameScreen>();
        List<GameScreen> screensToUpdate = new List<GameScreen>();
        public const int CHARACTER_NOT_INIT = -1;
        // DEBUGGING FUNCTIONS
        public int NumScreens
        {
            get { return screens.Count; }
        }
        public int NumScreensHidden
        {
            get
            {
                int numHidden = 0;
                foreach (GameScreen screen in screens)
                {
                    numHidden += (screen.ScreenState == ScreenState.Hidden) ? 1 : 0;
                }
                return numHidden;
            }
        }

        InputState input = new InputState();

        SpriteBatch spriteBatch;
        SpriteFont font;
        Texture2D blankTexture;
        

        Texture2D avatar1;
        Texture2D avatar2;
        List<Texture2D> avatars;

        List<Texture2D> backgrounds;       
        
        //Constant textures that are potentially needed for every slide
        Texture2D backgroundExtraPages;
        Texture2D menu_circleHighlight;
        Texture2D menu_sideDock;
        Texture2D menu_sideIcons_active;
        Texture2D menu_sideIcons_idle;

        Texture2D narrationOn;
        //Constant Rectangles for the menus in consitant locations!
        Rectangle sideMenuDockRect; 
        Rectangle sideMenuIconsRect;
        Rectangle fullscreen;

        //Our skeleton tracker for the program
       //       SkeletonTracker skeleton;
        KinectSensor kinect;
        Skeleton[] skeletonData;
        Skeleton curSkeleton;
        Skeleton curSkeleton2;
        SkeletonJoints curSkeletonJoints;
        SkeletonJoints curSkeletonJoints2;

        //Avatar variables
        Character curCharacter;
        Character curCharacter2;
        public int curAvatarIndex; //the index for which avatar is being drawn.
        public int curAvatarIndex2;
        private int numAvatars; //number of avatars that are availiable for use
        private int numBackgrounds; //number of backgrounds that are availiable for use

        //SpeechRecognizer for our program
        SpeechRecognizer speechRecognizer;
        public bool speechRecognitionOn = true;

        //Gesture Recognizer for our program
        DTWImplementation dtw;

        /*Default to have player 1 be our current player. This is needed because without specifing 
         * that a single player has control over a slide, the program assumes that all 4 players
         * have control over the slide, and any voice command is issued 4x or more! (ie "new" creates 4+ slides!)
         */
       public PlayerIndex currentPlayerIndex = PlayerIndex.One; 
        MainGestureMenu mainGestureMenu;

        public MainGestureMenu MainGestureMenu
        {
            get { return mainGestureMenu; }
        }
        
        bool isInitialized;

        bool traceEnabled;

        #endregion

        #region Properties ie Getters
       
        /// <summary>
        /// returns the number of avatars availiable in the program
        /// </summary>
        /// 
        public int NumberOfCharacters
        {
            get { return numAvatars; }
        }

        /// <summary>
        /// returns the number of backgrounds availiable in the program
        /// </summary>
        /// 
        public int NumberOfBackgrounds
        {
            get { return numBackgrounds; }
        }
        /// <summary>
        /// returns the current skeleton of the program
        /// </summary>
        /// 
       
        public Skeleton CurSkeleton
        {
            get { return curSkeleton; }
        }

        public Skeleton CurSkeleton2
        {
            get { return curSkeleton2; }
        }

        /// <summary>
        /// returns the current Character instance of the program
        /// </summary>
        /// 
        public Character CurCharacter
        {
            get { return curCharacter; }
        }

        public Character CurCharacter2
        {
            get { return curCharacter2; }
        }

        /// <summary>
        /// returns the current Character instance of the program
        /// </summary>
        /// 
        public int CurAvatarIndex
        {
            get { return curAvatarIndex; }
        }
        public int CurAvatarIndex2
        {
            get { return curAvatarIndex2; }
        }

        /// <summary>
        /// <summary>
        /// 
        /// returns the skeleton tracker of the program
        /// </summary>
        public SpeechRecognizer SpeechRecognizer
        {
            get { return speechRecognizer; }
        }

        /// <summary>
        /// 
        /// returns the texture 2D representing narration recording
        /// </summary>
        public Texture2D NarrationOn
        {
            get { return narrationOn; }
        }
        /// <summary>
        /// A default blankTexture shared by all the screens. This saves
        /// each screen(and especially the skeleton) having to bother creating their own local instance.
        /// </summary>
        public Texture2D BlankTexture
        {
            get { return blankTexture; }
        }

        
        /// <summary>
        /// A default SpriteBatch shared by all the screens. This saves
        /// each screen having to bother creating their own local instance.
        /// </summary>
        public SpriteBatch SpriteBatch
        {
            get { return spriteBatch; }
        }
        
        public KinectSensor Kinect
        {
          //  get { return skeleton.Kinect; }
            get { return kinect; }
        }
        /*
        public SkeletonTracker CurSkeletonTracker
        {
            get { return skeleton; }
        }
     */
        //Method to get the texture of the current skeleton becuase of class privacy compatabilities
       /*
        public Texture2D CurSkeletonTexture()
        {
            return skeleton.Head.Texture;
        }
        * */
        /*
        public void SetCurSkeletonTexture(int textureIndex)
        {
            if (textureIndex >= 0 && textureIndex < avatars.Count)
            {
                skeleton.Head.Texture = avatars[textureIndex];
            }
        }
        */
        
        /*
        //Method to get the rectangle of the current skeleton becuase of class privacy compatabilities
        public Rectangle CurSkeletonRectangle()
        {
            return skeleton.Head.Rectangle;
        }
         * */
        //Method to get the color of the current skeleton becuase of class privacy compatabilities\
        /*
        public Color CurSekeltonColor()
        {
            return skeleton.Head.Color;
        }
        */
        
        /// <summary>
        /// A default font shared by all the screens. This saves
        /// each screen having to bother loading their own local copy.
        /// </summary>
        public SpriteFont Font
        {
            get { return font; }
        }
        #region Drawing Gets for texture2Ds and rectangles needed to draw
       
        /// <summary>
        /// The following methods return default texture2Ds that many slides
        /// need. This saves each screen having to bother loading their own local copy.
        /// </summary>
        public Texture2D BackgroundExtraPages
        {
            get { return backgroundExtraPages; }
        }
        public Texture2D Menu_SideDock
        {
            get { return menu_sideDock; }
        }
        public Texture2D Menu_SideIcons_Active
        {
            get { return menu_sideIcons_active; }
        }
        public Texture2D Menu_SideIcons_Idle
        {
            get { return menu_sideIcons_idle; }
        }
        public Texture2D Menu_CircleHighlight
        {
            get { return menu_circleHighlight; }
        }
        public Rectangle SideMenuDockRectangle
        {
            get { return sideMenuDockRect; }
        }
        public Rectangle fullscreenRectangle
        {
            get { return fullscreen; }
        }
        public Rectangle SideMenuIconsRectangle
        {
            get { return sideMenuIconsRect; }
        }
        /// <summary>
        /// A default avatar shared by all the screens. This saves
        /// each screen having to bother loading their own local copy.
        /// </summary>
        public Texture2D Avatar1
        {
            get { return avatar1; }
        }
        #endregion
        /// <summary>
        /// If true, the manager prints out a list of all the screens
        /// each time it is updated. This can be useful for making sure
        /// everything is being added and removed at the right times.
        /// </summary>
        public bool TraceEnabled
        {
            get { return traceEnabled; }
            set { traceEnabled = value; }
        }
        
       
        
        #endregion

        #region Initialization


        /// <summary>
        /// Constructs a new screen manager component.
        /// </summary>
        public ScreenManager(Game game)
            : base(game)
        {
            // we must set EnabledGestures before we can query for them, but
            // we don't assume the game wants to read them.
            TouchPanel.EnabledGestures = GestureType.None;
            avatars = new List<Texture2D>();
            backgrounds = new List<Texture2D>();

           
        }


        /// <summary>
        /// Initializes the screen manager component.
        /// </summary>
        public override void Initialize()
        {
            base.Initialize();
            isInitialized = true;
        }


        /// <summary>
        /// Load your graphics content.
        /// </summary>
        protected override void LoadContent()
        {
            // Load content belonging to the screen manager.
            ContentManager content = Game.Content;
            #region Init Kinect
            if (KinectSensor.KinectSensors.Count > 0)
            {
                kinect = KinectSensor.KinectSensors[0];
                if (kinect == null)
                    throw new Exception("Kinect not plugged in!");
                TransformSmoothParameters p = new TransformSmoothParameters
                {
                    Smoothing = 0.75f,
                    Correction = 0.0f,
                    Prediction = 0.0f,
                    JitterRadius = 0.05f,
                    MaxDeviationRadius = 0.04f
                };
                
                kinect.SkeletonStream.Enable(p);
                // kinectSensor.SkeletonStream.

                kinect.Start();
                
                kinect.SkeletonFrameReady += new EventHandler<SkeletonFrameReadyEventArgs>(KinectSkeletonFramesReady);
                dtw = new DTWImplementation(kinect, this);
                this.skeletonData = new Skeleton[kinect.SkeletonStream.FrameSkeletonArrayLength];
            }
            #endregion

            spriteBatch = new SpriteBatch(GraphicsDevice);
            font = content.Load<SpriteFont>("menufont");
            blankTexture = content.Load<Texture2D>("blank");
            InitAvatars(content);

            curAvatarIndex = 0;
            curAvatarIndex2 = 1;
            curCharacter = new Character(this.Game, spriteBatch);
            curCharacter2 = new Character(this.Game, spriteBatch);
            curCharacter.setKinect(this.kinect);
            curCharacter2.setKinect(this.kinect);
            String[] avatarFilepaths = new String[] { 
                "characters/knight",
                "characters/pocahontas",
                "characters/cartoonGirl",
                "characters/dragon",
                "characters/white_knight", 
                "characters/bubble_man",
                
            };
            numAvatars = avatarFilepaths.Length;
            curCharacter.load(avatarFilepaths);
            curCharacter2.load(avatarFilepaths);

            //Texture2D av1 = new Texture2D(GraphicsDevice, 50, 100);
            // skeleton = new SkeletonTracker(this);


            mainGestureMenu = new MainGestureMenu(GraphicsDevice, content, curCharacter, curCharacter2, this);
            // Start speech recognizer after KinectSensor.Start() is called
            // returns null if problem with speech prereqs or instantiation.
            speechRecognizer = SpeechRecognizer.Create();
            speechRecognizer.SaidSomething += this.RecognizerSaidSomething;
            // speechRecognizer.Start(skeleton.Kinect.AudioSource);
            if (!(kinect == null))
                speechRecognizer.Start(kinect.AudioSource);
           
            InitTextures(content);
            InitRectangles(content);

            // Tell each of the screens to load their content.
            foreach (GameScreen screen in screens)
            {
                screen.LoadContent();
            }
        }
        private void InitTextures(ContentManager content)
        {
            //initialize background related textures. Add background textures to the backgrounds list
            backgroundExtraPages = content.Load<Texture2D>("backgrounds/behindPages_straightened");
            Texture2D beach = content.Load<Texture2D>("places menu/places/beach/o.beach");
            Texture2D coral = content.Load<Texture2D>("places menu/places/coralreef/o.coralreef");
            Texture2D fantasy = content.Load<Texture2D>("places menu/places/fantasy house/o.fantastyhouse");
            Texture2D monument = content.Load<Texture2D>("places menu/places/monument park/o.monumentpark");
            Texture2D rainforest = content.Load<Texture2D>("places menu/places/rainforest/o.rainforest");
            Texture2D snowy = content.Load<Texture2D>("places menu/places/snowy forest/o.snowyforest");
            Texture2D desert = content.Load<Texture2D>("places menu/places/desert pyramids/o.desert");
            backgrounds.Add(beach);
            backgrounds.Add(coral);
            backgrounds.Add(fantasy);
            backgrounds.Add(monument);
            backgrounds.Add(rainforest);
            backgrounds.Add(snowy);
            backgrounds.Add(desert);

            numBackgrounds = backgrounds.Count;
            
            //initialize menu textures
            menu_circleHighlight = content.Load<Texture2D>("menu/menu_circleHighlight");
            menu_sideDock = content.Load<Texture2D>("menu/menu_sideDock");
            menu_sideIcons_active = content.Load<Texture2D>("menu/menu_sideIcons_active");
            menu_sideIcons_idle = content.Load<Texture2D>("menu/menu_sideIcons_idle");

            narrationOn = content.Load<Texture2D>("narration/recording_hot");
        }

        private void InitRectangles(ContentManager content)
        {
            int sideMenuWidth = 75;
            int dockWidth = 40;
            int sideMenuIconOffset_X = 0;
            int sideMenuIconOffset_Y = 10;
            sideMenuDockRect = new Rectangle(0, 0, dockWidth, GraphicsDevice.Viewport.Height);
            sideMenuIconsRect = new Rectangle(sideMenuIconOffset_X, sideMenuIconOffset_Y, sideMenuWidth,  GraphicsDevice.Viewport.Height);
            fullscreen = new Rectangle(0, 0,  GraphicsDevice.Viewport.Width,  GraphicsDevice.Viewport.Height);
        }

        private void InitAvatars(ContentManager content)
        {
            avatar1 = content.Load<Texture2D>("knight");
             avatar2 = content.Load<Texture2D>("batman");
           
          //  avatar1 = content.Load<Texture2D>("blank");
            Texture2D avatar3 = content.Load<Texture2D>("princess");
            Texture2D avatar4 = content.Load<Texture2D>("questionIcon");
            avatars.Add(avatar1);
            avatars.Add(avatar2);
            avatars.Add(avatar3);
            avatars.Add(avatar4);
        }
        /// <summary>
        /// Unload your graphics content.
        /// </summary>
        protected override void UnloadContent()
        {
            // Tell each of the screens to unload their content.
            foreach (GameScreen screen in screens)
            {
                screen.UnloadContent();
            }
        }

        
        #endregion
        
        #region speech recognition
        private void RecognizerSaidSomething(object sender, SpeechRecognizer.SaidSomethingEventArgs e)
        {
            //return if speech recognition is turned off or if there are no slides on the screen(there are just the game screen and SlideMenuScreen);
            if (!speechRecognitionOn || this.screens.Count <= 2)
                return;
            if (this.screens.Count > 1)
                ((SlideScreen)this.screens[screens.Count - 1]).SlideRecognizerSaidSomething(sender, e);
            /*switch (e.Verb)
            {

                case SpeechRecognizer.Verbs.Capture:
                    Console.WriteLine("Recognized 'Capture'!!!!!!!!!!!!!!!!!!");
                    //if(this.screens[screens.Count-1].
                    //if(this.screens.

                    break;

                case SpeechRecognizer.Verbs.Pause:
                    Console.WriteLine("Recognized 'Pause'");
                    break;
                case SpeechRecognizer.Verbs.Resume:
                    Console.WriteLine("Recognized 'Resume'");
                    break;
                case SpeechRecognizer.Verbs.Reset:
                    Console.WriteLine("Recognized 'Reset'");
                    break;
                case SpeechRecognizer.Verbs.Faster:
                    Console.WriteLine("Recognized 'Faster'");
                    break;
                case SpeechRecognizer.Verbs.Slower:
                    Console.WriteLine("Recognized 'Slower'");
                    break;
                default:
                    Console.WriteLine("Recognizer said something Unknown");
                    break;
            }*/
        }
         #endregion
         

        #region Update and Draw


        /// <summary>
        /// Allows each screen to run logic.
        /// </summary>
        public override void Update(GameTime gameTime)
        {
            // Read the keyboard and gamepad.
            input.Update();

            // Make a copy of the master screen list, to avoid confusion if
            // the process of updating one screen adds or removes others.
            screensToUpdate.Clear();

            foreach (GameScreen screen in screens)
                screensToUpdate.Add(screen);

            bool otherScreenHasFocus = !Game.IsActive;
            bool coveredByOtherScreen = false;

            // Loop as long as there are screens waiting to be updated.
            while (screensToUpdate.Count > 0)
            {
                // Pop the topmost screen off the waiting list.
                GameScreen screen = screensToUpdate[screensToUpdate.Count - 1];

                screensToUpdate.RemoveAt(screensToUpdate.Count - 1);

                // Update the screen.
                screen.Update(gameTime, otherScreenHasFocus, coveredByOtherScreen);

                if (screen.ScreenState == ScreenState.TransitionOn ||
                    screen.ScreenState == ScreenState.Active)
                {
                    // If this is the first active screen we came across,
                    // give it a chance to handle input.
                    if (!otherScreenHasFocus)
                    {
                        screen.HandleInput(input);

                        otherScreenHasFocus = true;
                    }

                    // If this is an active non-popup, inform any subsequent
                    // screens that they are covered by it.
                    if (!screen.IsPopup)
                        coveredByOtherScreen = true;
                }
            }

            // Print debug trace?
            if (traceEnabled)
                TraceScreens();
        }


        /// <summary>
        /// Prints a list of all the screens, for debugging.
        /// </summary>
        void TraceScreens()
        {
            List<string> screenNames = new List<string>();

            foreach (GameScreen screen in screens)
                screenNames.Add(screen.GetType().Name);

            Debug.WriteLine(string.Join(", ", screenNames.ToArray()));
        }


        /// <summary>
        /// Tells each screen to draw itself.
        /// </summary>
        ///  
      
        public override void Draw(GameTime gameTime)
        {
            foreach (GameScreen screen in screens)
            {
                if (screen.ScreenState == ScreenState.Hidden)
                    continue;

                screen.Draw(gameTime);
            }

            //draws the main gesture menu
            mainGestureMenu.Draw(gameTime);

            spriteBatch.Begin();
            //  draw main avatar
            if (curSkeleton != null)
            {
                curSkeletonJoints.UpdateJointPositions(curSkeleton);
                curCharacter.update(curSkeletonJoints);
                curCharacter.draw(curAvatarIndex);
            }

            if (curSkeleton2 != null)
            {
                curSkeletonJoints2.UpdateJointPositions(curSkeleton2);
                curCharacter2.update(curSkeletonJoints2);
                curCharacter2.draw(curAvatarIndex2);
            }
            spriteBatch.End();

            
            //skeleton.Draw(gameTime);
        }


        #endregion
        #region KinectSkeletonAllFramesReady
        public void KinectSkeletonFramesReady(object sender, SkeletonFrameReadyEventArgs e)
        {
            Skeleton unusedSkeleton = null;
            Skeleton unusedSkeleton2 = null;
            bool skel1Set = false;
            bool skel2Set = false;
            using (SkeletonFrame sf = e.OpenSkeletonFrame())
            {
                if (sf != null)
                {
                    sf.CopySkeletonDataTo(skeletonData);

                    foreach (Skeleton skeleton in skeletonData)
                    {
                        //  take skeleton data and update avatar state

                        if (skeleton != null && skeleton.TrackingState == SkeletonTrackingState.Tracked)
                        {
                            if (!skel1Set && skeleton.TrackingId == curCharacter.SkeletonId)
                            {
                                curSkeleton = skeleton;
                                curSkeletonJoints = new SkeletonJoints(curSkeleton);
                                skel1Set = true;
                            }
                            else if (!skel2Set && skeleton.TrackingId == curCharacter2.SkeletonId)
                            {
                                curSkeleton2 = skeleton;
                                curSkeletonJoints2 = new SkeletonJoints(curSkeleton2);
                                skel2Set = true;
                            }
                            else if ((!skel1Set || !skel2Set) && unusedSkeleton == null)
                            {
                                unusedSkeleton = skeleton;
                            } else if ((!skel1Set || !skel2Set) && unusedSkeleton != null)
                            {
                                unusedSkeleton2 = skeleton;
                            }
                        }
                    }
                    if (unusedSkeleton != null)
                    {
                        if (!skel1Set)
                        {
                            curSkeleton = unusedSkeleton;
                            curSkeletonJoints = new SkeletonJoints(curSkeleton);
                            skel1Set = true;
                            curCharacter.SkeletonId = curSkeleton.TrackingId;
                        }
                        else if (!skel2Set)
                        {
                            curSkeleton2 = unusedSkeleton;
                            curSkeletonJoints2 = new SkeletonJoints(curSkeleton2);
                            skel2Set = true;
                            curCharacter2.SkeletonId = curSkeleton2.TrackingId;
                        }
                    }
                    if (unusedSkeleton2 != null)
                    {
                        if (!skel1Set)
                        {
                            curSkeleton = unusedSkeleton2;
                            curSkeletonJoints = new SkeletonJoints(curSkeleton);
                            skel1Set = true;
                        }
                        else if (!skel2Set)
                        {
                            curSkeleton2 = unusedSkeleton2;
                            curSkeletonJoints2 = new SkeletonJoints(curSkeleton2);
                            skel2Set = true;
                        }
                    }
                    if (!skel1Set)
                        curCharacter.SkeletonId = -1;
                    if (!skel2Set)
                        curCharacter2.SkeletonId = -1;

                }
            }

        }
        #endregion

        #region Public Methods
        ///<summary>
        /// ChangeAvatar
        /// changes the avatar to the avatar skin at index
        /// </summary>        
        public void CycleAvatar()
        {
            if (curAvatarIndex == numAvatars - 1)
                curAvatarIndex = 0;
            else
                curAvatarIndex++;
        }
        public void CycleAvatar(int skel_id)
        {
            if (curCharacter.SkeletonId == skel_id)
            {
                if (curAvatarIndex == numAvatars - 1)
                {
                    curAvatarIndex = 0;
                }
                else
                {
                    curAvatarIndex++;
                }
            }
            else if (curCharacter2.SkeletonId == skel_id)
            {
                if (curAvatarIndex2 == numAvatars - 1)
                {
                    curAvatarIndex2 = 0;
                }
                else
                {
                    curAvatarIndex2++;
                }
            }
        }

        public void ChangeAvatar(int index, PlayerIndex pi)
        {
            if (pi == PlayerIndex.One)
                curAvatarIndex = index;
            else if (pi == PlayerIndex.Two)
                curAvatarIndex2 = index;
        }

        ///<summary>
        /// ChangeAvatar
        /// changes the avatar to the avatar skin at index
        /// </summary>        
        public void ChangeAvatar(int index, int skel_id)
        {
            if (curCharacter.SkeletonId == skel_id)
                curAvatarIndex = index;
            else if (curCharacter2.SkeletonId == skel_id)
                curAvatarIndex2 = index;
        }
      
        /// getBackgroundScene returns the Texture2D of the background scene
        /// indicated by index. 
        /// </summary>
        public Texture2D GetBackgroundScene(int index)
        {
            return backgrounds[index];
        }

        /// <summary>
        /// Adds a new screen to the screen manager.
        /// </summary>
        public void AddScreen(GameScreen screen, PlayerIndex? controllingPlayer)
        {
            screen.ControllingPlayer = controllingPlayer;
            screen.ScreenManager = this;
            screen.IsExiting = false;

            // If we have a graphics device, tell the screen to load content.
            if (isInitialized)
            {
                screen.LoadContent();
            }

            screens.Add(screen);

            // update the TouchPanel to respond to gestures this screen is interested in
            TouchPanel.EnabledGestures = screen.EnabledGestures;
        }


        /// <summary>
        /// Removes a screen from the screen manager. You should normally
        /// use GameScreen.ExitScreen instead of calling this directly, so
        /// the screen can gradually transition off rather than just being
        /// instantly removed.
        /// </summary>
        public void RemoveScreen(GameScreen screen)
        {
            // If we have a graphics device, tell the screen to unload content.
            if (isInitialized)
            {
                screen.UnloadContent();
            }

            screens.Remove(screen);
            screensToUpdate.Remove(screen);

            // if there is a screen still in the manager, update TouchPanel
            // to respond to gestures that screen is interested in.
            if (screens.Count > 0)
            {
                TouchPanel.EnabledGestures = screens[screens.Count - 1].EnabledGestures;
            }
        }


        /// <summary>
        /// Expose an array holding all the screens. We return a copy rather
        /// than the real master list, because screens should only ever be added
        /// or removed using the AddScreen and RemoveScreen methods.
        /// </summary>
        public GameScreen[] GetScreens()
        {
            return screens.ToArray();
        }


        /// <summary>
        /// Helper draws a translucent black fullscreen sprite, used for fading
        /// screens in and out, and for darkening the background behind popups.
        /// </summary>
        public void FadeBackBufferToBlack(float alpha)
        {
            Viewport viewport = GraphicsDevice.Viewport;

            spriteBatch.Begin();

            spriteBatch.Draw(blankTexture,
                             new Rectangle(0, 0, viewport.Width, viewport.Height),
                             Color.Black * alpha);

            spriteBatch.End();
        }

        


        #endregion
    }
}
