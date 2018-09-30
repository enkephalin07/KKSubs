# KKSubs
Multilanguage captioning for KK


This has gone through so many changes that it required a new messagepack location (now in BepInEx\translation\)
and new config section (now [org.bepinex.kk.KKSubs]).

Changes: Most of the original Patchwork script's features have been implemented, minus remote editing (for now).
Added:

* Support for multiple speakers, multiple display lines, included the speaker's name for each line.
* Support for translation from external plugins, so languages other than English can be translated.
* Built in support for Japanese display; a Japanese reader can view subtitles without ever downloading or
  connecting to remote sources.
* Reduced dictionary memory to dialog loaded per scene and required for scene conditions. Dictionaries
  unload at the end of the scene, so you don't have to run around the world map with a dictionary of
  240k string elements slowing you down.
* Scene logging options, so translators and users can review and edit the translations. No support yet
  for translation from local files.

Planned: support for more sources, more tools for translators, text mesh, support for Motion Voices
and Talk Interlude, and speech bubble display.
