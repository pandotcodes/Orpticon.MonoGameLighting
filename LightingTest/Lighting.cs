using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.Extended;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace Orpticon.MonoGameLighting
{
    public static class Lighting
    {
        public static Rectangle Extend(this Rectangle rect, float factor)
        {
            int extensionX = (int)(rect.Width * factor - rect.Width);
            int extensionY = (int)(rect.Height * factor - rect.Height);
            rect.X -= extensionX / 2;
            rect.Y -= extensionY / 2;
            rect.Width += extensionX;
            rect.Height += extensionY;
            return rect;
        }
        public static RectangleF Extend(this RectangleF rect, float factor)
        {
            float extensionX = (rect.Width * factor - rect.Width);
            float extensionY = (rect.Height * factor - rect.Height);
            rect.X -= extensionX / 2;
            rect.Y -= extensionY / 2;
            rect.Width += extensionX;
            rect.Height += extensionY;
            return rect;
        }
        private static Texture2D pixelTexture;
        public static float MaxOpacity { get; set; } = 0.5f; // Maximum opacity
        public static float BaseDarkness { get; set; } = 0.95f;
        private static Rectangle CameraRect;
        //public static float CalculationExtensionFactor = 2;
        public static float[] AlphaMap;
        public static void Initialize(GraphicsDeviceManager graphicsDevice, Rectangle cameraRect)
        {
            // TODO: Add your initialization logic here

            pixelTexture = new Texture2D(graphicsDevice.GraphicsDevice, 1, 1);
            pixelTexture.SetData(new Color[] { Color.White });

            CameraRect = cameraRect;
            //CameraRect = CameraRect.Extend(CalculationExtensionFactor);

            AlphaMap = new float[CameraRect.Width * CameraRect.Height];
            Array.Fill(AlphaMap, 1f);
        }
        public static bool IsLit(Matrix camera, Vector2 point, bool allowCheckNearby = false) => IsLit(camera, point, out float darkness, allowCheckNearby);
        public static bool IsLit(Matrix camera, Vector2 point, out float darkness, bool allowCheckNearby = false)
        {
            var pos = point.ToPoint() - CameraRect.Location;
            darkness = 1;
            if (!allowCheckNearby && !CameraRect.Contains(point)) return false;
            if (pos.X < 0 || pos.Y < 0 || pos.X >= CameraRect.Width || pos.Y >= CameraRect.Height)
            {
                if (!allowCheckNearby) return false;
                else
                {
                    while (pos.X < 0) pos.X++;
                    while (pos.Y < 0) pos.Y++;
                    while (pos.X >= CameraRect.Width) pos.X--;
                    while (pos.Y >= CameraRect.Height) pos.Y--;
                }
            }
            Debug.WriteLine(pos);
            var alpha = AlphaMap[pos.Y * CameraRect.Width + pos.X];
            darkness = alpha;
            return alpha < 1;
        }
        public static void RenderCone(SpriteBatch _spriteBatch, Vector2 originPoint, float radius) => RenderCone(_spriteBatch, originPoint, new Vector2(0, -1), radius, 360, new List<RectangleF>(), Color.White);
        public static void RenderCone(SpriteBatch _spriteBatch, Vector2 originPoint, float radius, Color color) => RenderCone(_spriteBatch, originPoint, new Vector2(0, -1), radius, 360, new List<RectangleF>(), color);
        public static void RenderCone(SpriteBatch _spriteBatch, Vector2 originPoint, float radius, IEnumerable<RectangleF> collisionBoxes) => RenderCone(_spriteBatch, originPoint, new Vector2(0, -1), radius, 360, collisionBoxes, Color.White);
        public static void RenderCone(SpriteBatch _spriteBatch, Vector2 originPoint, float radius, IEnumerable<RectangleF> collisionBoxes, Color color) => RenderCone(_spriteBatch, originPoint, new Vector2(0, -1), radius, 360, collisionBoxes, color);
        public static void RenderCone(SpriteBatch _spriteBatch, Vector2 originPoint, Vector2 direction, float coneRadius, float coneAngleInDegrees) => RenderCone(_spriteBatch, originPoint, direction, coneRadius, coneAngleInDegrees, new List<RectangleF>(), Color.White);
        public static void RenderCone(SpriteBatch _spriteBatch, Vector2 originPoint, Vector2 direction, float coneRadius, float coneAngleInDegrees, IEnumerable<RectangleF> collisionBoxes) => RenderCone(_spriteBatch, originPoint, direction, coneRadius, coneAngleInDegrees, collisionBoxes, Color.White);
        public static void RenderCone(SpriteBatch _spriteBatch, Vector2 originPoint, Vector2 notNormalizedDirection, float coneRadius, float coneAngleInDegrees, IEnumerable<RectangleF> collisionBoxes, Color color)
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
                Vector2 directionToBox = boxCenter - originPoint;
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
                    if (!CameraRect.Contains(x, y)) continue;
                    Vector2 blockCenter = new Vector2(x + 1 / 2, y + 1 / 2);
                    Vector2 toBlock = blockCenter - originPoint;
                    if (toBlock.LengthSquared() > radiusSquared)
                        continue;

                    float angleToBlock = (float)Math.Atan2(toBlock.Y, toBlock.X);
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
            AlphaMap[i] *= (1 - opacity);
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
            // Calculate direction and its inverse
            Vector2 direction = rayEnd - rayStart;
            float invDirX = 1.0f / direction.X;
            float invDirY = 1.0f / direction.Y;

            // Pre-compute intersection times for x and y boundaries
            float t1 = (rectangle.Left - rayStart.X) * invDirX;
            float t2 = (rectangle.Right - rayStart.X) * invDirX;
            float t3 = (rectangle.Top - rayStart.Y) * invDirY;
            float t4 = (rectangle.Bottom - rayStart.Y) * invDirY;

            // Sort near and far times
            if (t1 > t2) { var temp = t1; t1 = t2; t2 = temp; }
            if (t3 > t4) { var temp = t3; t3 = t4; t4 = temp; }

            // Check if the ray misses the rectangle
            if (t1 > t4 || t3 > t2) return false;

            // Calculate the times of intersection
            float tNear = Math.Max(t1, t3);
            float tFar = Math.Min(t2, t4);

            // Return true if there's a valid intersection range
            return tNear >= 0 && tFar >= 0 && tNear <= 1;
        }
        public static void ApplyAlphaMap(SpriteBatch _spriteBatch)
        {
            // Create a new texture with the same dimensions as the CameraRect
            Texture2D texture = new Texture2D(_spriteBatch.GraphicsDevice, CameraRect.Width, CameraRect.Height);

            // Create an array to hold the color data
            Color[] colorData = new Color[CameraRect.Width * CameraRect.Height];

            // Set the color data based on the AlphaMap
            for (int x = 0; x < CameraRect.Width; x++)
            {
                for (int y = 0; y < CameraRect.Height; y++)
                {
                    int i = y * CameraRect.Width + x;
                    colorData[i] = Color.Black * BaseDarkness * AlphaMap[i];
                }
            }

            // Set the color data for the texture
            texture.SetData(colorData);

            _spriteBatch.Draw(texture, new Rectangle(CameraRect.X, CameraRect.Y, CameraRect.Width, CameraRect.Height), Color.White);
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
