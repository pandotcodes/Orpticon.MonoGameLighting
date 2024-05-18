using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.Extended;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Orpticon.MonoGameLighting
{
    public static class Lighting
    {
        private static Texture2D pixelTexture;
        public static float MaxOpacity { get; set; } = 0.5f; // Maximum opacity
        public static float BaseDarkness { get; set; } = 0.95f;
        private static Rectangle CameraRect;
        private static float[] alphaMap;
        public static void Initialize(GraphicsDeviceManager graphicsDevice, Rectangle cameraRect)
        {
            // TODO: Add your initialization logic here

            pixelTexture = new Texture2D(graphicsDevice.GraphicsDevice, 1, 1);
            pixelTexture.SetData(new Color[] { Color.White });

            CameraRect = cameraRect;

            alphaMap = new float[CameraRect.Width * CameraRect.Height];
            Array.Fill(alphaMap, 1f);
        }

        public static void RenderCone(SpriteBatch _spriteBatch, Vector2 originPoint, float radius) => RenderCone(_spriteBatch, originPoint, new Vector2(0, -1), radius, 360, new List<RectangleF>(), Color.White);
        public static void RenderCone(SpriteBatch _spriteBatch, Vector2 originPoint, float radius, Color color) => RenderCone(_spriteBatch, originPoint, new Vector2(0, -1), radius, 360, new List<RectangleF>(), color);
        public static void RenderCone(SpriteBatch _spriteBatch, Vector2 originPoint, float radius, List<RectangleF> collisionBoxes) => RenderCone(_spriteBatch, originPoint, new Vector2(0, -1), radius, 360, collisionBoxes, Color.White);
        public static void RenderCone(SpriteBatch _spriteBatch, Vector2 originPoint, float radius, List<RectangleF> collisionBoxes, Color color) => RenderCone(_spriteBatch, originPoint, new Vector2(0, -1), radius, 360, collisionBoxes, color);
        public static void RenderCone(SpriteBatch _spriteBatch, Vector2 originPoint, Vector2 direction, float coneRadius, float coneAngleInDegrees) => RenderCone(_spriteBatch, originPoint, direction, coneRadius, coneAngleInDegrees, new List<RectangleF>(), Color.White);
        public static void RenderCone(SpriteBatch _spriteBatch, Vector2 originPoint, Vector2 direction, float coneRadius, float coneAngleInDegrees, List<RectangleF> collisionBoxes) => RenderCone(_spriteBatch, originPoint, direction, coneRadius, coneAngleInDegrees, collisionBoxes, Color.White);
        public static void RenderCone(SpriteBatch _spriteBatch, Vector2 originPoint, Vector2 notNormalizedDirection, float coneRadius, float coneAngleInDegrees, List<RectangleF> collisionBoxes, Color color)
        {
            float halfAngle = MathHelper.ToRadians(coneAngleInDegrees) / 2;
            Vector2 normalizedDirection = Vector2.Normalize(notNormalizedDirection);
            float coneStartAngle = (float)Math.Atan2(normalizedDirection.Y, normalizedDirection.X) - halfAngle;
            float coneEndAngle = (float)Math.Atan2(normalizedDirection.Y, normalizedDirection.X) + halfAngle;

            Vector2 startingPoint = new Vector2(
                Math.Min(originPoint.X, originPoint.X + normalizedDirection.X * coneRadius) - coneRadius,
                Math.Min(originPoint.Y, originPoint.Y + normalizedDirection.Y * coneRadius) - coneRadius
            );

            float radiusSquared = coneRadius * coneRadius;
            float maxBoundingCircleRadius = (float)Math.Sqrt(2 * (1f / 2) * (1f / 2)); // Diagonal of a block

            List<RectangleF> filteredCollisionBoxes = new List<RectangleF>();

            foreach (var collisionBox in collisionBoxes)
            {
                Vector2 boxCenter = new Vector2(collisionBox.Center.X, collisionBox.Center.Y);
                Vector2 directionToBox = Vector2.Normalize(boxCenter - originPoint);
                float distanceToBoxCenter = Vector2.Distance(originPoint, boxCenter);
                if (distanceToBoxCenter > coneRadius + maxBoundingCircleRadius)
                    continue;

                float angleToBox = (float)Math.Atan2(directionToBox.Y, directionToBox.X);
                if (angleToBox < 0)
                    angleToBox += MathHelper.TwoPi;

                float normalizedConeStartAngle = coneStartAngle < 0 ? coneStartAngle + MathHelper.TwoPi : coneStartAngle;
                float normalizedConeEndAngle = coneEndAngle < 0 ? coneEndAngle + MathHelper.TwoPi : coneEndAngle;

                bool withinConeAngle = normalizedConeStartAngle <= angleToBox && angleToBox <= normalizedConeEndAngle;
                if (normalizedConeEndAngle < normalizedConeStartAngle)
                    withinConeAngle = angleToBox >= normalizedConeStartAngle || angleToBox <= normalizedConeEndAngle;

                if (withinConeAngle)
                {
                    filteredCollisionBoxes.Add(collisionBox);
                }
            }

            for (float y = startingPoint.Y; y < originPoint.Y + coneRadius; y += 1)
            {
                for (float x = startingPoint.X; x < originPoint.X + coneRadius; x += 1)
                {
                    if(!CameraRect.Contains(x, y)) continue;
                    Vector2 blockCenter = new Vector2(x + 1 / 2, y + 1 / 2);
                    Vector2 toBlock = blockCenter - originPoint;
                    if (toBlock.LengthSquared() > radiusSquared)
                        continue;

                    Vector2 directionToBlock = Vector2.Normalize(toBlock);
                    float angleToBlock = (float)Math.Atan2(directionToBlock.Y, directionToBlock.X);
                    if (angleToBlock < 0)
                        angleToBlock += MathHelper.TwoPi;

                    float normalizedConeStartAngle = coneStartAngle < 0 ? coneStartAngle + MathHelper.TwoPi : coneStartAngle;
                    float normalizedConeEndAngle = coneEndAngle < 0 ? coneEndAngle + MathHelper.TwoPi : coneEndAngle;

                    bool withinConeAngle = normalizedConeStartAngle <= angleToBlock && angleToBlock <= normalizedConeEndAngle;
                    if (normalizedConeEndAngle < normalizedConeStartAngle)
                        withinConeAngle = angleToBlock >= normalizedConeStartAngle || angleToBlock <= normalizedConeEndAngle;

                    if (withinConeAngle)
                    {
                        bool clearLineOfSight = true;
                        foreach (var collisionBox in filteredCollisionBoxes)
                        {
                            if (collisionBox.Contains(blockCenter) || RayIntersectsRectangle(originPoint, blockCenter, collisionBox))
                            {
                                clearLineOfSight = false;
                                break;
                            }
                        }

                        if (clearLineOfSight)
                        {
                            float distanceToCenter = toBlock.Length();
                            float opacity = MaxOpacity * (1 - MathHelper.Clamp(distanceToCenter / coneRadius, 0f, 1f));
                            DrawFilledRectangle((int)x, (int)y, opacity);
                        }
                    }
                }
            }
        }

        private static void DrawFilledRectangle(int x, int y, float opacity)
        {
            int i = (y - CameraRect.Y) * CameraRect.Width + (x - CameraRect.X);
            alphaMap[i] *= (1 - opacity);
        }

        private static bool PointInCone(Vector2 point, Vector2 originPoint, Vector2 endPoint, float coneRadius, float angleOffset)
        {
            Vector2 directionToPoint = point - originPoint;
            Vector2 directionToEndpoint = endPoint - originPoint;
            float dotProduct = Vector2.Dot(directionToPoint, directionToEndpoint);

            if (dotProduct < 0)
                return false; // Point is behind the origin point of the cone

            float distanceSquaredToOrigin = Vector2.DistanceSquared(point, originPoint);
            float maxDistanceSquared = coneRadius * coneRadius;

            if (distanceSquaredToOrigin > maxDistanceSquared)
                return false; // Point is outside cone radius

            float angleToPoint = (float)Math.Acos(dotProduct / (directionToPoint.Length() * directionToEndpoint.Length()));
            return angleToPoint <= angleOffset;
        }
        private static bool RayIntersectsRectangle(Vector2 rayStart, Vector2 rayEnd, RectangleF rectangle)
        {
            Vector2 direction = rayEnd - rayStart;
            float invDirX = 1.0f / direction.X;
            float invDirY = 1.0f / direction.Y;

            float tNearX = (rectangle.Left - rayStart.X) * invDirX;
            float tNearY = (rectangle.Top - rayStart.Y) * invDirY;
            float tFarX = (rectangle.Right - rayStart.X) * invDirX;
            float tFarY = (rectangle.Bottom - rayStart.Y) * invDirY;

            if (tNearX > tFarX) (tNearX, tFarX) = (tFarX, tNearX);
            if (tNearY > tFarY) (tNearY, tFarY) = (tFarY, tNearY);

            if (tNearX > tFarY || tNearY > tFarX)
                return false;

            float tNear = Math.Max(tNearX, tNearY);
            float tFar = Math.Min(tFarX, tFarY);

            return tNear >= 0 && tFar >= 0 && tNear <= 1;
        }
        public static void ApplyAlphaMap(SpriteBatch _spriteBatch)
        {
            for(int x = 0; x < CameraRect.Width; x++)
            {
                for(int y = 0; y < CameraRect.Height; y++)
                {
                    int i = y * CameraRect.Width + x;
                    _spriteBatch.Draw(pixelTexture, new Rectangle(x + CameraRect.X, y + CameraRect.Y, 1, 1), Color.Black * BaseDarkness * alphaMap[i]);
                }
            }
        }
        //protected override void Draw(GameTime gameTime)
        //{
        //    GraphicsDevice.Clear(Color.Green);

        //    _spriteBatch.Begin();
        //    var pos = Mouse.GetState().Position.ToVector2();
        //    var cen = new Vector2(_graphics.PreferredBackBufferWidth / 2, _graphics.PreferredBackBufferHeight / 2);

        //    var rects = new List<RectangleF> { new RectangleF(pos, new Vector2(48, 48)), new RectangleF(new Vector2(1024, 384), new Vector2(48, 48)) };
        //    rects.ForEach(x => DrawFilledRectangle(_spriteBatch, x, Color.Brown));

        //    RenderCone(_spriteBatch, new Vector2(cen.X, cen.Y), 1000, rects);
        //    _spriteBatch.End();

        //    base.Draw(gameTime);
        //}
    }
}
