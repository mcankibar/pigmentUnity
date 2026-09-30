PIGMENT — Unity port of liquidGame

Open Assets/Pigment/Scenes/Pigment.unity and press Play, then BAŞLA.
Click a pigment glass. Hold to tilt and pour; release to stop increasing the tilt.
The selected glass stays above the beaker. Click another glass to switch.
The level ends at the dashed fill line, after in-flight liquid has landed.
85% = one star, 90% = two, 95% = three. All seven levels end in a celebration.
Mouse and primary touch are supported. UI buttons do not pour liquid.

AUTHORING
Assets/Pigment/Config/BenchLayout.asset:
  mixer position/scale; source group position, arc width/depth/spread;
  per-slot offsets and scale multipliers; shelf position/scale/width/tilt;
  automatic or manual camera placement and portrait/landscape framing.
  Z values follow the original Three.js configuration (positive toward player).
  The runtime converts Z to Unity's coordinate convention.
  Changing layout in Play mode rebuilds the current level and resets its mix.
  Outside Play mode use Pigment > Apply Layout to Scene Preview, then save.

Assets/Pigment/Config/PourAndScoring.asset:
  fill line, source supply, angle/speed/spring/damping, flow, gravity,
  stream thickness and star thresholds.
Assets/Pigment/Config/VisualsAndAudio.asset:
  glass/liquid materials, font, tutorial hand, lights, effects and sound levels.
Assets/Pigment/Config/Levels/: seven editable recipe + source-order assets.
  Recipe XYZW = red, yellow, blue, white. Values are ratios.
Assets/Pigment/Config/Vessels/: six vessel definitions and prefab references.
Assets/Pigment/Prefabs/: beaker, sample, tumbler, highball, tulip, rocks.

SOURCE MAPPING
mix.js -> PigmentMath: RYB cube, white dilution, OKLab similarity.
vessels.js -> VesselDefinition, PigmentMath.Cavity, VesselMesh, LiquidSurface.
bench.js -> PigmentGame, VesselView, PourStream.
levels.js/session.js -> PigmentLevel assets, PigmentGame state + stars.
defaultGameConfig.js -> BenchLayout, PourAndScoring, VisualsAndAudio.
hud.js/css -> editable Canvas hierarchy + PigmentHud, TextMeshPro, safe area.
workshop.js -> procedural oak, stone, parchment, books and particles.
audio.js -> PigmentAudio: synthesized workshop music, trickle and glass chimes.

The renderer uses native URP materials and procedural clipped liquid meshes.
Three.js transmission/refraction, CSS backdrop blur and Web Audio filters are
recreated with Unity equivalents, not pixel-identical shader/audio internals.
Playable-ad export/network SDKs belong to the web host and are not part of this
standalone Unity game's runtime.

VALIDATION
Pigment > Validate Math and Levels
Pigment.Editor.PigmentValidation.SourceParity(): 50 original JavaScript
color/scoring fixtures and 36 original vessel volume/tilt fixtures.
Pigment.Editor.PigmentValidation.Playtest(): in Play mode, pours actual sources
through all seven levels and reports the final scores; no direct score injection.

The original SampleScene is preserved. No source liquidGame files are modified.
