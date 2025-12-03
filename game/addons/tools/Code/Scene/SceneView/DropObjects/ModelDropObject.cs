using Sandbox.ModelEditor.Nodes;
using System.Threading;

namespace Editor;

[DropObject( "model", "vmdl", "vmdl_c" )]
partial class ModelDropObject : BaseDropObject
{
	Model model;
	string archetype;

	protected override async Task Initialize( string dragData, CancellationToken token )
	{
		Asset asset = await InstallAsset( dragData, token );

		if ( asset is null )
			return;

		if ( token.IsCancellationRequested )
			return;

		archetype = asset.FindStringEditInfo( "model_archetype_id" );

		PackageStatus = "Loading Model";
		model = await Model.LoadAsync( asset.Path );
		PackageStatus = null;

		Bounds = model.Bounds;
		PivotPosition = Bounds.ClosestPoint( Vector3.Down * 10000 );
	}

	public override void OnUpdate()
	{
		using var scope = Gizmo.Scope( "DropObject", traceTransform );

		Gizmo.Draw.Color = Color.White.WithAlpha( 0.3f );
		Gizmo.Draw.LineBBox( Bounds );

		Gizmo.Draw.Color = Color.White;

		if ( model is not null )
		{
			var so = Gizmo.Draw.Model( model );
			if ( so.IsValid() )
			{
				so.Flags.CastShadows = true;
			}
		}

		if ( !string.IsNullOrWhiteSpace( PackageStatus ) )
		{
			Gizmo.Draw.Text( PackageStatus, new Transform( Bounds.Center ), "Inter", 14 * Application.DpiScale );

			Gizmo.Draw.Color = Color.White.WithAlpha( 0.3f );
			Gizmo.Draw.Sprite( Bounds.Center + Vector3.Up * 12, 16, "materials/gizmo/downloads.png" );
		}
	}

	private bool HasPropData()
	{
		if ( archetype == "physics_prop_model" || archetype == "jointed_physics_model" || archetype == "breakable_prop_model" )
			return true;
		if ( model.Data.Explosive )
			return true;
		if ( model.Data.Flammable )
			return true;
		if ( model.Data.Health > 0 )
			return true;
		if ( model.HasData<ModelBreakPiece[]>() )
			return true;
		return false;
	}

	public override async Task OnDrop()
	{
		await WaitForLoad();

		if ( model is null )
			return;

		using var scene = SceneEditorSession.Scope();

		using ( SceneEditorSession.Active.UndoScope( "Drop Model" ).WithGameObjectCreations().Push() )
		{
			GameObject = new GameObject( false );
			GameObject.Name = model.ResourceName;
			GameObject.WorldTransform = traceTransform;

			bool physics = (model.Physics?.Parts.Count ?? 0) > 0;
			if ( physics && HasPropData() )
			{
				var prop = GameObject.Components.Create<Prop>();
				prop.Model = model;
				prop.IsStatic = archetype == "" || archetype == "default" || archetype == "static_prop_model" || archetype == "animated_model";
			}
			else if ( model.BoneCount > 0 )
			{
				var renderer = GameObject.Components.Create<SkinnedModelRenderer>();
				renderer.Model = model;
			}
			else
			{
				var renderer = GameObject.Components.Create<ModelRenderer>();
				renderer.Model = model;
				if ( physics )
				{
					var collider = GameObject.Components.Create<ModelCollider>();
					collider.Model = model;
				}
			}

			GameObject.Enabled = true;

			EditorScene.Selection.Clear();
			EditorScene.Selection.Add( GameObject );
		}
	}
}
