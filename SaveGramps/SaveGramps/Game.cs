// #define DDEBUG

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;
using Microsoft.Xna.Framework.Media;
using SaveGramps.GameObjects;
using GrandpaBrain;

namespace SaveGramps
{
    enum GameStates
    {
        Start,
        RefreshLevel,
        PlayLevel,
        RoundReward,
        RoundEnd,
        EndLevel
    }

    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class Game : Microsoft.Xna.Framework.Game
    {
        static Random gRandom = new Random();
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;
        Texture2D ballTexture;
        Texture2D backgroundTexture;
        Texture2D wakeUpGrandpa;
        SpriteFont roundFont;
        List<Ball> balls ;
        HUD hud;
        SpriteFont arialFont;
        GameStates gameState = GameStates.Start;
        int lvHandler;
        Answer answerInBrain;
        AudioManager audioManager = new AudioManager();
        TimeSpan roundRewardMessageTimeout = new TimeSpan(0, 0, 1);
        TimeSpan accumulateTime = new TimeSpan(0, 0, 0);
        Texture2D grampsHead;
        Rectangle startPos;
        Rectangle aboutPos;
        Rectangle settingPos;

        bool drawMessage = false;
        bool winOrLose = false;
        public Game()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";

            // Frame rate is 30 fps by default for Windows Phone.
            TargetElapsedTime = TimeSpan.FromTicks(333333);

            // Extend battery life under lock.
            InactiveSleepTime = TimeSpan.FromSeconds(1);
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            DefaultLevel defaultLv = new DefaultLevel();
            lvHandler = Generator.RegisterLevel(defaultLv);
            base.Initialize();
            hud = new HUD();
            audioManager.Initialize(Content);
            audioManager.playBgMusic();
            hud.tx2WakeUpGrandPa = wakeUpGrandpa;
            hud.wakeUpTotal = 3;
            startPos = new Rectangle(550, 50, 112, 112);
            aboutPos = new Rectangle(550, 350, 112, 112);
            settingPos = new Rectangle(550, 200, 112, 112);
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);
            ballTexture = Content.Load<Texture2D>("smallballcolorshadow");
            arialFont = Content.Load<SpriteFont>("Arial");
            roundFont = Content.Load<SpriteFont>("RoundFont");
            backgroundTexture = Content.Load<Texture2D>("background");
            grampsHead = Content.Load<Texture2D>("gramps_big");
            wakeUpGrandpa = Content.Load<Texture2D>("gramps_small");
            Ball.Initialize(ballTexture);
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

            // TODO: Add your update logic here
            Ball hitBall = null;
            TouchCollection touchCollection = TouchPanel.GetState();
            switch (gameState)
            {
                case GameStates.Start:
                    foreach (TouchLocation tl in touchCollection)
                    {
                        if (startPos.Contains((int)tl.Position.X, (int)tl.Position.Y))
                        {
                            gameState = GameStates.RefreshLevel;
                        }
                    }
                    break;
                case GameStates.RefreshLevel:
                    {
                        UpdateRewardMessageTime(gameTime);

                        int maxRightPosition = graphics.GraphicsDevice.Viewport.Width - Ball.Texture.Width;

                        balls = new List<Ball>();

                        // query balls from Grandpa's Brain
                        Response expectedResponse = Generator.GetExpectedResponseByLevel(lvHandler);

                        hud.desiredTotal = expectedResponse.Answer;
                        answerInBrain = new Answer(expectedResponse);

                        int numOfBalls = expectedResponse.Numbers.Count() + expectedResponse.Operands.Count();
                        int viewWidth = graphics.GraphicsDevice.Viewport.Width - Ball.Texture.Width;
                        int sizeOfDivision = viewWidth / numOfBalls;

                        int buffer = sizeOfDivision / 3;
                        List<int> positions = new List<int>();
                        for (int i = 0; i < numOfBalls; i++)
                        {
                            int startPos = i * sizeOfDivision;
                            int xVal = gRandom.Next(startPos + buffer, (startPos + sizeOfDivision) - buffer);
                            positions.Add(xVal);
                        }

                        int xPos = 0;
#if DDEBUG
                        int i = 0;
#endif

                        foreach(var num in expectedResponse.Numbers)
                        {
#if DDEBUG
                            Vector2 position = new Vector2(100 * i, 100); i++;
#else
                            xPos = HelperMethods.GetRandomElement<int>(positions);
                            Vector2 position = new Vector2(xPos, 485);
#endif
                            Ball ball = new NumberBall(
                                    num,
                                    position,
                                    (position.X > this.graphics.GraphicsDevice.Viewport.Width / 2) ? -1 : 1
                                    );
                            balls.Add(ball);
                        }

#if DDEBUG
                        i = 0;
#endif

                        foreach (var op in expectedResponse.Operands)
                        {

#if DDEBUG
                            Vector2 position = new Vector2(100 * i, 300); i++;
#else
                            xPos = HelperMethods.GetRandomElement<int>(positions);
                            Vector2 position = new Vector2(xPos, 485);
#endif
                            Ball ball = new OperatorBall(
                                    op,
                                    position,
                                    (position.X > this.graphics.GraphicsDevice.Viewport.Width / 2) ? -1 : 1
                                    );
                            balls.Add(ball);                            
                        }

                        gameState = GameStates.PlayLevel;
                        break;
                    }
                case GameStates.RoundReward:
                    TerminateCond term; string termMsg;
                    bool isTerminate = answerInBrain.ShouldTerminate(out term, out termMsg);
                    if (isTerminate && term == TerminateCond.Impossible)
                    {
                        // TODO: GOT to study ur SAT penality comes in
                        hud.wakeUpTotal--;
                    }
                    if (hud.wakeUpTotal == 0)
                        gameState = GameStates.RefreshLevel; // TODO: update this to go to show the angel
                    else
                        gameState = GameStates.RoundEnd;

                    UpdateRewardMessageTime(gameTime);
                    UpdateAndRemoveOutOfBoundBalls(gameTime);
                    if (balls.Count == 0)
                    {
                        gameState = GameStates.RefreshLevel;
                    }
                    break;
                case GameStates.RoundEnd:
                    UpdateRewardMessageTime(gameTime);
                    UpdateAndRemoveOutOfBoundBalls(gameTime);
                    if (balls.Count == 0)
                    {
                        gameState = GameStates.RefreshLevel;
                    }
                    break;
                case GameStates.PlayLevel:
                    {
                        UpdateRewardMessageTime(gameTime);

                        foreach (TouchLocation tl in touchCollection)
                        {
                            if ((tl.State == TouchLocationState.Pressed)
                                /* || (tl.State == TouchLocationState.Moved)*/)
                            {
                                foreach (Ball ball in balls)
                                {
                                    if (ball.Hit(tl.Position.X, tl.Position.Y))
                                    {
                                        hitBall = ball;
                                        break;
                                    }
                                }
                            }
                        }
                        if (hitBall != null)
                        {
                            // call AddNumber or AddOperand
                            if (hitBall.ballType == BallType.Number)
                            {
                                answerInBrain.AddNumber(int.Parse(hitBall.text));
                            }
                            else if (hitBall.ballType == BallType.Operand)
                            {
                                answerInBrain.AddOperand(OperandHelper.ConvertStringToOperands(hitBall.text));
                            }
                            else
                            {
                                throw new Exception("The hitBall.BallType is not yet implemented");
                            }
                            audioManager.playBallPopped();
                            hitBall.KillBall();
                            //balls.Remove(hitBall);

                        }

                        // check if answer is correct or
                        string _termMsg;
                        TerminateCond cond;
                        if (answerInBrain.ShouldTerminate(out cond, out _termMsg))
                        {
                            switch (cond)
                            {
                                case TerminateCond.Normal:
                                    hud.runningTotal = hud.runningTotal + 1;
                                    drawMessage = true;
                                    winOrLose = true;
                                    foreach (Ball b in balls)
                                    {
                                        b.KillBall();
                                    }
                                    //gameState = GameStates.RefreshLevel;
                                    gameState = GameStates.RoundReward;
                                    break;
                                case TerminateCond.Impossible: //update to a display this new picture
                                    drawMessage = true;
                                    winOrLose = false;
                                    foreach (Ball b in balls)
                                    {
                                        b.KillBall();
                                    }
                                    gameState = GameStates.RoundReward;
                                    break;
                                case TerminateCond.Timeout:
                                    throw new Exception("Timeout");
                                    gameState = GameStates.RefreshLevel;
                                    break;
                                case TerminateCond.NoTerminate:
                                    throw new Exception("NoTerminate");
                                    break;
                            }
                        }

                        // TODO: end game state when balls left screen
                        // Update ball locations
                        UpdateAndRemoveOutOfBoundBalls(gameTime);
                        
                        if (balls.Count == 0)
                        {
                            gameState = GameStates.RefreshLevel;
                        }
                        break;
                    }
                case GameStates.EndLevel:
                    break;
                default:
                    break;
            }

            base.Update(gameTime);
        }

        private void UpdateRewardMessageTime(GameTime gameTime)
        {
            if (drawMessage)
            {
                this.accumulateTime += gameTime.ElapsedGameTime;
                if (this.accumulateTime >= this.roundRewardMessageTimeout)
                {
                    drawMessage = false;
                    accumulateTime = new TimeSpan(0, 0, 0);
                }
            }
        }

        protected void DrawSettingsBall(string text, Rectangle position)
        {
            float offsetX = ballTexture.Width / 2 + position.X;
            float offsetY = ballTexture.Height / 2 + position.Y;
            Vector2 fontCenter = new Vector2(offsetX, offsetY);
            spriteBatch.Draw(ballTexture, position, Color.White);
            spriteBatch.DrawString(arialFont, text, fontCenter, Color.White, 0, arialFont.MeasureString(text) / 2, 1.0f, SpriteEffects.None, 0.5f);
        }
        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            spriteBatch.Begin();
            spriteBatch.Draw(backgroundTexture, Vector2.Zero, Color.White);
            spriteBatch.End();

            switch(gameState)
            {
                case GameStates.Start:
                {
                    spriteBatch.Begin();
                    spriteBatch.Draw(grampsHead, new Rectangle(0,0,500,480), Color.White);
                    DrawSettingsBall("Start", startPos);
                    DrawSettingsBall("About", aboutPos);
                    DrawSettingsBall("Settings", settingPos);
                    spriteBatch.End();
                    break;
                }
                case GameStates.RefreshLevel:
                {
                    DrawRewardMessage();
                    break;
                }
                case GameStates.RoundReward:
                case GameStates.RoundEnd:
                case GameStates.PlayLevel:
                {
                    spriteBatch.Begin();
                    spriteBatch.Draw(grampsHead, new Vector2(0, 350), Color.White);
                    spriteBatch.End();

                    DrawRewardMessage();
                    spriteBatch.Begin();
                    foreach (Ball ball in balls)
                    {
                        ball.Draw(arialFont, spriteBatch);
                    }
                    hud.Draw(arialFont, spriteBatch, gameTime);
                    hud.DrawRoundTotal(arialFont, spriteBatch);
                    spriteBatch.End();
                    break;
                }
                case GameStates.EndLevel:
                {
                    break;
                }
            }

            base.Draw(gameTime);
        }

        private void DrawRewardMessage()
        {
            if (drawMessage)
            {
                spriteBatch.Begin();
                if (winOrLose)
                {
                    spriteBatch.DrawString(arialFont, "Good Job!", new Vector2(400, 140), Color.White);
                }
                else
                {
                    spriteBatch.DrawString(arialFont, "Oops, try again!", new Vector2(400, 140), Color.White);
                }
                spriteBatch.End();
            }
        }

        private void UpdateAndRemoveOutOfBoundBalls(GameTime gameTime)
        {
            for (int i = balls.Count - 1; i >= 0; i--)
            {
                Ball ball = balls[i];
#if DDEBUG
                            //ball.Update(gameTime);
#else
                ball.Update(gameTime);
#endif

                // check if ball is off the screen
                if (((ball.position.X + Ball.Texture.Width) <= 0) || (ball.position.X > graphics.GraphicsDevice.Viewport.Width) ||
                    (ball.position.Y > graphics.GraphicsDevice.Viewport.Height + 10))
                {
                    balls.RemoveAt(i);
                }

            }
        }
    }

    public static class HelperMethods
    {
        private static Random random = new Random();

        public static T GetRandomElement<T>(this List<T> list)
        {
            if (list.Count() == 0)
            {
                throw new Exception("no more elements in list");
            }
            int pos = random.Next(list.Count());
            T result = list[pos];
            list.RemoveAt(pos);
            return result;
        }
    }
}
