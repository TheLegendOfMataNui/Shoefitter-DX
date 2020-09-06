using System;
using System.Windows.Input;
using SharpDX;

namespace ShoefitterDX.Renderer
{
    public class PreviewCamera
    {
        public Vector3 Position = Vector3.Zero;
        public float FocusDepth = 10; // Depth of rotation center for 3rd person mode.
        public float Pitch = 0;
        public float Yaw = 0;
        // Ignore Roll for now. Who would ever roll their preview camera?
        public float AspectRatio = 1.0f;
        public float FOV = (float)Math.PI / 3; // 60 degrees.
        public float ZNear = 0.1f;
        public float ZFar = 40000;
        public bool FirstPerson = false;

        public Vector3 CameraUp
        {
            get
            {
                return (Vector3)Vector3.Transform(Vector3.UnitY, Matrix.RotationX(Pitch) * Matrix.RotationY(Yaw)) * new Vector3(1, 1, -1);
            }
        }

        public Vector3 CameraLeft

        {
            get
            {
                return (Vector3)Vector3.Transform(-Vector3.UnitX, Matrix.RotationY(-Yaw));
            }
        }

        public Vector3 CameraForward
        {
            get
            {
                return (Vector3)Vector3.Transform(Vector3.UnitZ, Matrix.RotationX(-Pitch) * Matrix.RotationY(-Yaw));
            }
        }

        public PreviewCamera()
        {

        }

        public PreviewCamera(Vector3 Position)
        {
            this.Position = Position;
        }

        public Matrix ViewMatrix
        {
            get
            {
                if (FirstPerson)
                {
                    return Matrix.Translation(-Position) * Matrix.RotationY(Yaw) * Matrix.RotationX(Pitch);
                }
                else
                {
                    return Matrix.Translation(-Position) * Matrix.RotationY(Yaw) * Matrix.RotationX(Pitch) * Matrix.Translation(0, 0, FocusDepth);
                }
            }
        }

        public Matrix ProjectionMatrix
        {
            get
            {
                return Matrix.PerspectiveFovLH(FOV, AspectRatio, ZNear, ZFar);
            }
        }
    }

    #region Camera Controllers
    public abstract class CameraController
    {
        public PreviewCamera Camera { get; }

        public CameraController(PreviewCamera camera)
        {
            this.Camera = camera;
        }

        public virtual bool KeyDown(Key key)
        {
            return false;
        }

        public virtual bool KeyUp(Key key)
        {
            return false;
        }

        public virtual bool MouseDown(MouseButton button)
        {
            return false;
        }

        public virtual bool MouseUp(MouseButton button)
        {
            return false;
        }

        public virtual bool MouseMove(float x, float y)
        {
            return false;
        }

        public virtual bool MouseWheel(int delta)
        {
            return false;
        }

        public virtual void Update(float timeStep)
        {

        }
    }

    public class OrbitCameraController : CameraController
    {
        public bool IsAltDown { get; private set; } = false;
        public bool IsOrbiting { get; private set; } = false;
        public bool IsPanning { get; private set; } = false;
        public bool IsZooming { get; private set; } = false;

        private float LastX;
        private float LastY;

        public OrbitCameraController(PreviewCamera camera) : base(camera)
        {

        }

        public override bool KeyDown(Key key)
        {
            if (key == Key.LeftAlt)
            {
                //Debug.WriteLine("[Orbit Camera] Alt key down!");
                IsAltDown = true;
                return true;
            }
            return false;
        }

        public override bool KeyUp(Key key)
        {
            if (key == Key.LeftAlt)
            {
                //Debug.WriteLine("[Orbit Camera] Alt key up!");
                IsAltDown = false;
                return true;
            }
            return false;
        }

        public override bool MouseDown(MouseButton button)
        {
            IsAltDown = Keyboard.IsKeyDown(Key.LeftAlt);

            if (IsAltDown && button == MouseButton.Left)
            {
                IsOrbiting = true;
                return true;
            }
            else if (IsAltDown && button == MouseButton.Middle)
            {
                IsPanning = true;
                return true;
            }
            else if (IsAltDown && button == MouseButton.Right)
            {
                IsZooming = true;
                return true;
            }
            else
            {
                return false;
            }
        }

        public override bool MouseMove(float x, float y)
        {
            bool result = false;

            float dx = x - LastX;
            float dy = y - LastY;

            if (IsOrbiting)
            {
                Camera.Yaw += -dx * 3.0f;
                Camera.Pitch += -dy * 4.0f;

                if (Camera.Pitch > MathUtil.PiOverTwo)
                {
                    Camera.Pitch = MathUtil.PiOverTwo;
                }
                if (Camera.Pitch < -MathUtil.PiOverTwo)
                {
                    Camera.Pitch = -MathUtil.PiOverTwo;
                }
                result = true;
            }

            if (IsPanning)
            {
                Camera.Position += Camera.CameraLeft * dx * Camera.FocusDepth;
                Camera.Position += Camera.CameraUp * dy * Camera.FocusDepth;
                result = true;
            }

            if (IsZooming)
            {
                Camera.FocusDepth += Camera.FocusDepth * dy;
                result = true;
            }

            LastX = x;
            LastY = y;

            return result;
        }

        public override bool MouseUp(MouseButton button)
        {
            if (IsOrbiting && button == MouseButton.Left)
            {
                IsOrbiting = false;
                return true;
            }
            else if (IsPanning && button == MouseButton.Middle)
            {
                IsPanning = false;
                return true;
            }
            else if (IsZooming && button == MouseButton.Right)
            {
                IsZooming = false;
                return true;
            }
            else
            {
                return false;
            }
        }

        public override bool MouseWheel(int delta)
        {
            Camera.FocusDepth += Camera.FocusDepth * -((float)delta / 120.0f) * 0.3f;
            return true;
        }
    }

    public class FirstPersonCameraController : CameraController
    {
        public bool IsRotating { get; private set; } = false;
        public float MovementSpeed { get; set; } = 3.0f;
        public float SprintScale { get; set; } = 10.0f;

        private float LastX;
        private float LastY;

        public FirstPersonCameraController(PreviewCamera camera) : base(camera)
        {

        }

        public override bool MouseDown(MouseButton button)
        {
            if (button == MouseButton.Right)
            {
                IsRotating = true;
                return true;
            }
            return false;
        }

        public override bool MouseMove(float x, float y)
        {
            bool result = false;

            float dx = x - LastX;
            float dy = y - LastY;

            if (IsRotating)
            {
                Camera.Yaw += -dx * 3.0f;
                Camera.Pitch += -dy * 4.0f;

                if (Camera.Pitch > MathUtil.PiOverTwo)
                {
                    Camera.Pitch = MathUtil.PiOverTwo;
                }
                if (Camera.Pitch < -MathUtil.PiOverTwo)
                {
                    Camera.Pitch = -MathUtil.PiOverTwo;
                }
                result = true;
            }

            LastX = x;
            LastY = y;

            return result;
        }

        public override bool MouseUp(MouseButton button)
        {
            if (button == MouseButton.Right)
            {
                IsRotating = false;
                return true;
            }
            return false;
        }

        public override void Update(float timeStep)
        {
            float speed = MovementSpeed * timeStep;
            if (Keyboard.IsKeyDown(Key.LeftShift))
            {
                speed *= SprintScale;
            }
            if (Keyboard.IsKeyDown(Key.Space))
            {
                Camera.Position += Vector3.UnitY * speed;
            }
            if (Keyboard.IsKeyDown(Key.LeftCtrl))
            {
                Camera.Position += -Vector3.UnitY * speed;
            }
            if (Keyboard.IsKeyDown(Key.W))
            {
                Camera.Position += Camera.CameraForward * speed;
            }
            if (Keyboard.IsKeyDown(Key.S))
            {
                Camera.Position += -Camera.CameraForward * speed;
            }
            if (Keyboard.IsKeyDown(Key.A))
            {
                Camera.Position += Camera.CameraLeft * speed;
            }
            if (Keyboard.IsKeyDown(Key.D))
            {
                Camera.Position += -Camera.CameraLeft * speed;
            }
        }
    }
    #endregion
}