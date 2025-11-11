using Godot;
using System;
using System.Threading.Tasks;

public partial class Book : Node3D {
    private int currentPageNumber = 1;
    private Node3D staticPage;
    private Node3D turningPage;
    private AnimationPlayer turningAnimation;
    private MeshInstance3D pf1;
    private MeshInstance3D pf2;
    private MeshInstance3D pf3;
    private MeshInstance3D pf4;
    private MeshInstance3D ps1;
    private MeshInstance3D ps2;
    private Viewport v1;
    private Viewport v2;
    private Viewport v3;
    private Viewport v4;
    private Viewport v5;
    private Viewport v6;
    private AudioStreamPlayer2D sfx;
    private AnimationPlayer animationPlayer;

    public override void _Ready() {
        staticPage = GetNode<Node3D>("Book/Static");
        this.turningPage = GetNode<Node3D>("Book/Turning");
        turningAnimation = GetNode<AnimationPlayer>("Book/Turning/AnimationPlayer");
        pf1 = GetNode<MeshInstance3D>("Book/Turning/PageLeft");
        pf2 = GetNode<MeshInstance3D>("Book/Turning/Page/Skeleton3D/Front");
        pf3 = GetNode<MeshInstance3D>("Book/Turning/Page/Skeleton3D/Back");
        pf4 = GetNode<MeshInstance3D>("Book/Turning/PageRight");
        ps1 = GetNode<MeshInstance3D>("Book/Static/PageLeft");
        ps2 = GetNode<MeshInstance3D>("Book/Static/PageRight");
        v1 = GetNode<Viewport>("Viewport1");
        v2 = GetNode<Viewport>("Viewport2");
        v3 = GetNode<Viewport>("Viewport3");
        v4 = GetNode<Viewport>("Viewport4");
        v5 = GetNode<Viewport>("Viewport5");
        v6 = GetNode<Viewport>("Viewport6");
        sfx = GetNode<AudioStreamPlayer2D>("AudioStreamPlayer2D");
        animationPlayer = GetNode<AnimationPlayer>("Book/Turning/AnimationPlayer");
        UpdatePageNumber();
        this.turningPage.Hide();
        this.SetTexture(ps1, v3);
        this.SetTexture(ps2, v4);
        animationPlayer.AnimationFinished += OnAnimationFinished;
    }

    public override void _Input(InputEvent @event) {
        if (turningAnimation.IsPlaying())
            return;
        if (Input.IsActionJustPressed("ui_left"))
            this.TurnLeft();
        if (Input.IsActionJustPressed("ui_right"))
            this.TurnRight();
    }

    private void TurnRight() {
        this.SetTexture(pf1, v3);
        this.SetTexture(pf2, v4);
        this.SetTexture(pf3, v5);
        this.SetTexture(pf4, v6);
        HideAndShow(pf4);
        staticPage.Hide();
        this.turningPage.Show();
        turningAnimation.Play("Turn1");
        sfx.Play();
    }

    private void TurnLeft() {
        if (currentPageNumber <= 1)
            return;
        this.SetTexture(pf1, v1);
        this.SetTexture(pf2, v2);
        this.SetTexture(pf3, v3);
        this.SetTexture(pf4, v4);
        HideAndShow(pf1);
        this.turningPage.Show();
        staticPage.Hide();
        turningAnimation.Play("Turn2");
        sfx.Play();
    }

    private async void HideAndShow(MeshInstance3D page) {
        page.Hide();
        await Task.Delay(100); 
        page.Show();
    }

    private void UpdatePageNumber(int pageOffset = 0) {
        currentPageNumber = Math.Max(1, currentPageNumber + pageOffset);
        int startPage = Math.Max(1, currentPageNumber - 2);
        Viewport[] viewports = { v1, v2, v3, v4, v5, v6 };
        for (int i = 0; i < viewports.Length; i++) {
            var page = viewports[i].GetNodeOrNull<Page>("Page");
            if (page == null) {
                GD.PushWarning($"{Name}: 视口 {viewports[i].Name} 中未找到 Page 组件，无法设置页码。");
                continue;
            }
            page.SetNumber(startPage + i);
        }
    }

    private void SetTexture(MeshInstance3D page, Viewport viewport) {
        var material = new StandardMaterial3D();
        material.AlbedoTexture = viewport.GetTexture();
        page.MaterialOverride = material;
    }

    private void OnAnimationFinished(StringName animName) {
        if (animName == "Turn1")
            UpdatePageNumber(2);
        if (animName == "Turn2")
            UpdatePageNumber(-2);
        staticPage.Show();
        this.turningPage.Hide();
    }
}