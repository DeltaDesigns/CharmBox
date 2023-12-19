# Changelog

## 1.0.0 (2023-12-19)


### Features

* **activity:** add location names to activities ([8eab199](https://github.com/DeltaDesigns/Charm/commit/8eab19977571bfbabe80d881e382f34464ec92b0))
* add custom renderer for latest destiny 2 static meshes ([#134](https://github.com/DeltaDesigns/Charm/issues/134)) ([0e8e955](https://github.com/DeltaDesigns/Charm/commit/0e8e9555a1466e8cb00d46febc295201eb9e9c5b))
* Beyond Light support ([#126](https://github.com/DeltaDesigns/Charm/issues/126)) ([fa30543](https://github.com/DeltaDesigns/Charm/commit/fa30543b16ec63776597ee74fc1a4540a128c260))
* **export:** Map entities are now exported with map ([#121](https://github.com/DeltaDesigns/Charm/issues/121)) ([2081f86](https://github.com/DeltaDesigns/Charm/commit/2081f8633e0b4eb7719b03ed9ef1ca28bdefb2db))
* refactor how extraction works to be more generic, allowing for fully synchronous fbx exporting ([#120](https://github.com/DeltaDesigns/Charm/issues/120)) ([d922a0b](https://github.com/DeltaDesigns/Charm/commit/d922a0b048d08f1d1b835ee0d79fdfe237fa89d2))
* saving textures now uses exporter system ([#137](https://github.com/DeltaDesigns/Charm/issues/137)) ([a86daaf](https://github.com/DeltaDesigns/Charm/commit/a86daaf03335e97ad98def8859c3051918b5c85b))
* Shader updates, add cubemaps to info cfg, export terrain statics, other misc things ([#116](https://github.com/DeltaDesigns/Charm/issues/116)) ([ed7428d](https://github.com/DeltaDesigns/Charm/commit/ed7428d39cb2386e84290fc39e6608836cb0a7a0))


### Bug Fixes

* add thread locking to entity loading to resolve some entity loading issues ([#125](https://github.com/DeltaDesigns/Charm/issues/125)) ([ebe0506](https://github.com/DeltaDesigns/Charm/commit/ebe0506ea1548f6395a1c70df9da2b316bd730ad))
* **audio:** Fix audio incorrect subtype for SK, NonSchemaType is now a StrategyAttribute ([6f7a3cc](https://github.com/DeltaDesigns/Charm/commit/6f7a3ccb9bfa96717530ac64adfe84e03ac14b3d))
* **audio:** fix weapon group audio not loading ([a3f2ae9](https://github.com/DeltaDesigns/Charm/commit/a3f2ae9ff65547ec76bb9e2003ddf7f157abca5c))
* correct dev menu hash64 ([ac8b78b](https://github.com/DeltaDesigns/Charm/commit/ac8b78b0719ab4ec3ce0bea45db3d80779a46ddc))
* entities now load bones and weights in new scene system ([#124](https://github.com/DeltaDesigns/Charm/issues/124)) ([00ec82d](https://github.com/DeltaDesigns/Charm/commit/00ec82d93db47f71d922b12b1c6a1df258c3fbd3))
* Fix a couple of crashes ([#131](https://github.com/DeltaDesigns/Charm/issues/131)) ([497b76b](https://github.com/DeltaDesigns/Charm/commit/497b76b0b66fbdb459b7ba238e07fdbbb1bdf4c7))
* Fix multiple entity points not being added ([#123](https://github.com/DeltaDesigns/Charm/issues/123)) ([6a71853](https://github.com/DeltaDesigns/Charm/commit/6a7185375e3ba552b390708700a621911a8aa651))
* fix rare crash when loading cubemaps with an invalid number of faces ([e342c5e](https://github.com/DeltaDesigns/Charm/commit/e342c5e4ab6a1a3a4b522a1ae33fd6250e8f455c))
* Fix unnamed weapon sounds for Lightfall ([#122](https://github.com/DeltaDesigns/Charm/issues/122)) ([791b36e](https://github.com/DeltaDesigns/Charm/commit/791b36e05b37e0440d8a7204113a52d1ff55d84a))
* named weapon sounds for Season 22 ([#127](https://github.com/DeltaDesigns/Charm/issues/127)) ([d7a8084](https://github.com/DeltaDesigns/Charm/commit/d7a80840cdb30b2872d2fb499879408f533879e9))
* Static mesh decals for Shadowkeep ([#135](https://github.com/DeltaDesigns/Charm/issues/135)) ([244508b](https://github.com/DeltaDesigns/Charm/commit/244508bde3648a65493e5192ff473526681b7e43))
* **statics:** fix statics not working in post-bl ([2e2e01f](https://github.com/DeltaDesigns/Charm/commit/2e2e01f085dcc95b363588321b9ab791d684647c))
* **statics:** Fixed static pre-bl issues with incorrect vertex layouts by using DXBC input semantics ([b68f47e](https://github.com/DeltaDesigns/Charm/commit/b68f47ec0cfebb063fcf6663e8fec53bbd30772a))
