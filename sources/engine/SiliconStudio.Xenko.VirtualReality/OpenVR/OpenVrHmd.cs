﻿using SiliconStudio.Core;
using SiliconStudio.Core.Mathematics;
using SiliconStudio.Xenko.Engine;
using SiliconStudio.Xenko.Games;
using SiliconStudio.Xenko.Graphics;
using SiliconStudio.Xenko.Rendering;
using SiliconStudio.Xenko.Rendering.Composers;

namespace SiliconStudio.Xenko.VirtualReality
{
    public class OpenVrHmd : Hmd
    {
        private RectangleF leftView = new RectangleF(0.0f, 0.0f, 0.5f, 1.0f);
        private RectangleF rightView = new RectangleF(0.5f, 0.0f, 1.0f, 1.0f);

        internal OpenVrHmd(IServiceRegistry registry) : base(registry)
        { 
            OpenVR.Init();
        }

        public override void Initialize(Entity cameraRoot, CameraComponent leftCamera, CameraComponent rightCamera)
        {
            var width = (int)(2160.0f*RenderFrameScaling);
            var height = (int)(1200*RenderFrameScaling);
            RenderFrameProvider = new DirectRenderFrameProvider(RenderFrame.FromTexture(Texture.New2D(GraphicsDevice, width, height, PixelFormat.R8G8B8A8_UNorm_SRgb, TextureFlags.RenderTarget | TextureFlags.ShaderResource)));

            var compositor = (SceneGraphicsCompositorLayers)Game.SceneSystem.SceneInstance.Scene.Settings.GraphicsCompositor;
            compositor.Master.Add(new SceneDelegateRenderer((x, y) =>
            {
                OpenVR.Submit(0, RenderFrameProvider.RenderFrame.RenderTargets[0], ref leftView);
                OpenVR.Submit(1, RenderFrameProvider.RenderFrame.RenderTargets[0], ref rightView);
            }));

            base.Initialize(cameraRoot, leftCamera, rightCamera);
        }

        public override void Draw(GameTime gameTime)
        {
            Vector3 pos, scale, camPos;
            Matrix rot, camRot;
            Matrix leftEye, rightEye, head, leftProj, rightProj;

            OpenVR.UpdatePoses();

            //have to make sure it's updated now
            CameraRootEntity.Transform.UpdateWorldMatrix();

            OpenVR.GetEyeToHead(0, out leftEye);
            OpenVR.GetEyeToHead(1, out rightEye);

            LeftEyePose = leftEye;
            RightEyePose = rightEye;

            State = OpenVR.GetHeadPose(out head);

            HeadPose = head;

            OpenVR.GetProjection(0, LeftCameraComponent.NearClipPlane, LeftCameraComponent.FarClipPlane, out leftProj);
            OpenVR.GetProjection(1, LeftCameraComponent.NearClipPlane, LeftCameraComponent.FarClipPlane, out rightProj);

            CameraRootEntity.Transform.WorldMatrix.Decompose(out scale, out camRot, out camPos);

            LeftCameraComponent.UseCustomProjectionMatrix = true;
            LeftCameraComponent.ProjectionMatrix = leftProj;

            var eyeMat = leftEye * head * ViewScaling * Matrix.Translation(camPos) * camRot;          
            eyeMat.Decompose(out scale, out rot, out pos);
            var finalUp = Vector3.TransformCoordinate(new Vector3(0, 1, 0), rot);
            var finalForward = Vector3.TransformCoordinate(new Vector3(0, 0, -1), rot);
            var view = Matrix.LookAtRH(pos, pos + finalForward, finalUp);
            LeftCameraComponent.UseCustomViewMatrix = true;
            LeftCameraComponent.ViewMatrix = view;

            RightCameraComponent.UseCustomProjectionMatrix = true;
            RightCameraComponent.ProjectionMatrix = rightProj;

            eyeMat = rightEye * head * ViewScaling * Matrix.Translation(camPos) * camRot;  
            eyeMat.Decompose(out scale, out rot, out pos);
            finalUp = Vector3.TransformCoordinate(new Vector3(0, 1, 0), rot);
            finalForward = Vector3.TransformCoordinate(new Vector3(0, 0, -1), rot);
            view = Matrix.LookAtRH(pos, pos + finalForward, finalUp);
            RightCameraComponent.UseCustomViewMatrix = true;
            RightCameraComponent.ViewMatrix = view;

            base.Draw(gameTime);
        }

        public override DeviceState State { get; protected set; }

        public override float RenderFrameScaling { get; set; } = 1.4f;

        public override Size2F RenderFrameSize => new Size2F(RenderFrameProvider.RenderFrame.Width, RenderFrameProvider.RenderFrame.Height);

        public override DirectRenderFrameProvider RenderFrameProvider { get; protected set; }

        public override Size2 OptimalRenderFrameSize => new Size2(2160, 1200);
    }
}