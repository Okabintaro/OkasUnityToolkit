# Okas Unity Toolkit

This project contains a loose collection of my scripts and tools that I use in my Unity projects.
Most of them are scripts that I made using ChatGPT and Sonnet with some tweaks here and there.
They helped me port Unreal Engine Assets, optimize and cleanup my projects more easily.

I will try to flesh them out together with some documentation as I go along.
For now its mainly a backup and convenience for me to use between all of my projects.

## Credits

The project contains some open source scripts with modifications I made to them.

- [roundyyy/intelligent_mesh_combiner: Intelligent Mesh Combiner](https://github.com/roundyyy/intelligent_mesh_combiner)
    - File: [IntelligentMeshCombiner.cs](Editor/Optimize/MeshDecimatorWindow.cs)
    - Made it not update with every change of setting value since since it was causing performance issues.
    - Made it unwrap the UVs of the combined mesh using Bakerys xatlas wrapper

## License

Except for the scripts that are credited, all the scripts in this project are licensed under the MIT License.
See the [LICENSE](LICENSE.md) file for more information.