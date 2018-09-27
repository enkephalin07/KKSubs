# hsubs
Multilanguage captioning for KK


KKSubs, currently alpha, won't be ready for release without more testing. This has gone through so
many changes that it required a new messagepack location (now in BepInEx\translation\) and new 
config section (now [org.bepinex.kk.KKSubs]).

Currently testing the smaller runtime dictionary for completeness in the scene, whether all required
voices are loaded at scene initialization. Also testing validation of cache contents with the list,
and aquiring all available ENG translations. When I'm fairly sure of these, this'll be ready for a
0.9 release. For 1.0 I would still need to implement UpdateMode.Scene, so lines can be aquired at
need from remote sources, and the cache could be validated with that. 

Planned: support for more sources, more tools for translators, text mesh, support for Motion Voices
and Talk Interlude, and speech bubble display.
