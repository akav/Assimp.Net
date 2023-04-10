/*
* Copyright (c) 2012-2020 AssimpNet - Nicholas Woodfield
* 
* Permission is hereby granted, free of charge, to any person obtaining a copy
* of this software and associated documentation files (the "Software"), to deal
* in the Software without restriction, including without limitation the rights
* to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
* copies of the Software, and to permit persons to whom the Software is
* furnished to do so, subject to the following conditions:
* 
* The above copyright notice and this permission notice shall be included in
* all copies or substantial portions of the Software.
* 
* THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
* IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
* FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
* AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
* LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
* OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
* THE SOFTWARE.
*/

using System;
using System.IO;
using System.Threading;
using System.Collections.Generic;
using Assimp.Configs;
using Assimp.Unmanaged;
using NUnit.Framework;

namespace Assimp.Test
{
    [TestFixture]
    public class AssimpContextTestFixture
    {

        [OneTimeSetUp]
        public void Setup()
        {
            var outputPath = Path.Combine(TestHelper.RootPath, "TestFiles/output");

            if (!Directory.Exists(outputPath))
                Directory.CreateDirectory(outputPath);

            IEnumerable<String> filePaths = Directory.GetFiles(outputPath);

            foreach(var filePath in filePaths)
            {
                if (File.Exists(filePath))
                    File.Delete(filePath);
            }
        }

        [Test, Parallelizable(ParallelScope.Self)]
        public void TestExportBadFormatId()
        {
            var context = new AssimpContext();
            var config = new NormalSmoothingAngleConfig(66.0f);
            context.SetConfig(config);

            // This is how you would use the log stream if writing to Sentry or some other logging service
            var logStream = new LogStream(delegate (string msg, string userData)
            {
                Console.Write($"{userData}: {msg}"); // Note that the newline is already included in the message
            }, "TestExportBadFormatId");
            logStream.Attach();

            var collada = context.ImportFile(Path.Combine(TestHelper.RootPath, "TestFiles/duck.dae"));

            var success = context.ExportFile(collada, Path.Combine(TestHelper.RootPath, "TestFiles/output/exportedCollada.dae"), "dae");
            Assert.That(success, Is.False);

            success = context.ExportFile(collada, Path.Combine(TestHelper.RootPath, "TestFiles/output/exportedCollada.dae"), "collada");
            Assert.That(success, Is.True);

            logStream.Detach();
        }

        [Test, Parallelizable(ParallelScope.Self)]
        public void TestExportToBlob()
        {
            var colladaPath = Path.Combine(TestHelper.RootPath, "TestFiles/duck.dae");

            var context = new AssimpContext();
            var ducky = context.ImportFile(colladaPath);
            Assert.That(ducky, Is.Not.Null);
            
            var blob = context.ExportToBlob(ducky, "obj");
            Assert.That(blob.HasData, Is.True);
            Assert.That(blob.NextBlob, Is.Not.Null);
            Assert.That(blob.NextBlob.Name, Is.EqualTo("mtl"));
        }

        [Test, Parallelizable(ParallelScope.Self)]
        public void TestImportExportFile()
        {
            var colladaPath = Path.Combine(TestHelper.RootPath, "TestFiles/duck.dae");
            var plyPath = Path.Combine(TestHelper.RootPath, "TestFiles/output/duck.ply");

            var context = new AssimpContext();
            var ducky = context.ImportFile(colladaPath);
            var success = context.ExportFile(ducky, plyPath, "ply");
            Assert.That(success, Is.True);
            Assert.That(File.Exists(plyPath), Is.True);
        }

        [Test, Parallelizable(ParallelScope.Self)]
        public void TestImportExportImportFile()
        {
            var colladaPath = Path.Combine(TestHelper.RootPath, "TestFiles/duck.dae");
            var plyPath = Path.Combine(TestHelper.RootPath, "TestFiles/output/duck2.dae");

            var context = new AssimpContext();
            var ducky = context.ImportFile(colladaPath);
            var success = context.ExportFile(ducky, plyPath, "collada");
            Assert.That(success, Is.True);
            
            var ducky2 = context.ImportFile(plyPath);
            Assert.That(ducky2, Is.Not.Null);
        }

        [Test, Parallelizable(ParallelScope.Self)]
        public void TestExportToFile()
        {
            var path = Path.Combine(TestHelper.RootPath, "TestFiles/ExportedTriangle.obj");
            var logStream = new TestContextLogStream();
            logStream.Attach();

            //Create a very simple scene a single node with a mesh that has a single face, a triangle and a default material
            var scene = new Scene();
            scene.RootNode = new Node("Root");

            var mesh = new Mesh("", PrimitiveType.Triangle);
            mesh.Vertices.Add(new Vector3D(1, 0, 0));
            mesh.Vertices.Add(new Vector3D(5, 5, 0));
            mesh.Vertices.Add(new Vector3D(10, 0, 0));
            mesh.Faces.Add(new Face(new[] { 0, 1, 2 }));
            mesh.MaterialIndex = 0;

            scene.Meshes.Add(mesh);
            scene.RootNode.MeshIndices.Add(0);

            var mat = new Material();
            mat.Name = "MyMaterial";
            scene.Materials.Add(mat);

            //Export the scene then read it in and compare!

            var context = new AssimpContext();
            var success = context.ExportFile(scene, path, "obj");
            Assert.That(success, Is.True);
            
            var importedScene = context.ImportFile(path);
            Assert.That(importedScene.MeshCount, Is.EqualTo(scene.MeshCount));
            Assert.That(importedScene.MaterialCount, Is.EqualTo(2)); //Always has the default material, should also have our material

            //Compare the meshes
            var importedMesh = importedScene.Meshes[0];
            Assert.That(importedMesh.VertexCount, Is.EqualTo(mesh.VertexCount));
            for(var i = 0; i < importedMesh.VertexCount; i++)
            {
                Assert.That(importedMesh.Vertices[i], Is.EqualTo(mesh.Vertices[i]));
            }

            Assert.IsTrue(importedMesh.FaceCount == mesh.FaceCount);
            for(var i = 0; i < importedMesh.FaceCount; i++)
            {
                var importedFace = importedMesh.Faces[i];
                var face = mesh.Faces[i];

                for(var j = 0; j < importedFace.IndexCount; j++)
                {
                    Assert.That(importedFace.Indices[j], Is.EqualTo(face.Indices[j]));
                }
            }
        }

        [Test, Parallelizable(ParallelScope.None)]
        public void TestFreeLogStreams()
        {
            Assert.That(LogStream.AttachedLogStreamCount, Is.Zero);
            var console1 = new ConsoleLogStream();
            var console2 = new ConsoleLogStream();
            var console3 = new ConsoleLogStream();

            console1.Attach();
            console2.Attach();
            console3.Attach();
            
            console1.Log("Test1");
            console2.Log("Test2");
            console3.Log("Test3");

            AssimpLibrary.Instance.FreeLibrary();

            var logs = LogStream.GetAttachedLogStreams();
            Assert.Multiple(() =>
            {
                Assert.That(logs, Is.Empty);
                Assert.That(console1.IsAttached, Is.False);
                Assert.That(console2.IsAttached, Is.False);
                Assert.That(console3.IsAttached, Is.False);
                Assert.That(LogStream.AttachedLogStreamCount, Is.Zero);
            });
        }

        [Test, Parallelizable(ParallelScope.Self)]
        public void TestImportFromFile()
        {
            var path = Path.Combine(TestHelper.RootPath, "TestFiles/sphere.obj");

            var context = new AssimpContext();
            context.SetConfig(new NormalSmoothingAngleConfig(55.0f));
            context.Scale = .5f;
            context.XAxisRotation = 25.0f;
            context.YAxisRotation = 50.0f;
            LogStream.IsVerboseLoggingEnabled = true;

            var success = context.ContainsConfig(NormalSmoothingAngleConfig.NormalSmoothingAngleConfigName);
            Assert.That(success, Is.True);

            context.RemoveConfigs();
            success = context.ContainsConfig(NormalSmoothingAngleConfig.NormalSmoothingAngleConfigName);
            Assert.That(success, Is.False);

            context.SetConfig(new NormalSmoothingAngleConfig(65.0f));
            context.SetConfig(new NormalSmoothingAngleConfig(22.5f));
            context.RemoveConfig(NormalSmoothingAngleConfig.NormalSmoothingAngleConfigName);

            success = context.ContainsConfig(NormalSmoothingAngleConfig.NormalSmoothingAngleConfigName);
            Assert.That(success, Is.False);

            context.SetConfig(new NormalSmoothingAngleConfig(65.0f));

            var scene = context.ImportFile(path, PostProcessPreset.TargetRealTimeMaximumQuality);

            Assert.That(scene, Is.Not.Null);
            Assert.That(scene.SceneFlags & SceneFlags.Incomplete, Is.Not.EqualTo(SceneFlags.Incomplete));
        }

        [Test, Parallelizable(ParallelScope.Self)]
        public void TestImportFromStream()
        {
            var path = Path.Combine(TestHelper.RootPath, "TestFiles/duck.dae");

            var context = new AssimpContext();
            LogStream.IsVerboseLoggingEnabled = true;

            var logStream = new TestContextLogStream();
            logStream.Attach();

            var fs = File.OpenRead(path);
            var scene = context.ImportFileFromStream(fs, ".dae");
            Assert.That(scene, Is.Not.Null);
            fs.Close();

            Assert.That(scene.SceneFlags & SceneFlags.Incomplete, Is.Not.EqualTo(SceneFlags.Incomplete));

            logStream.Detach();
        }

        [Test, Parallelizable(ParallelScope.Self)]
        public void TestImportFromStreamNoFormatHint()
        {
            var path = Path.Combine(TestHelper.RootPath, "TestFiles/duck.dae");
            
            var context = new AssimpContext();
            var logStream = new TestContextLogStream();
            logStream.Attach();
            LogStream.IsVerboseLoggingEnabled = true;

            var fs = File.OpenRead(path);
            var scene = context.ImportFileFromStream(fs, String.Empty); //null also seems to work well
            fs.Close();

            Assert.That(scene, Is.Not.Null);
            Assert.That(scene.SceneFlags & SceneFlags.Incomplete, Is.Not.EqualTo(SceneFlags.Incomplete));

            logStream.Detach();
        }

        [Test, Parallelizable(ParallelScope.Self)]
        public void TestImporterDescriptions()
        {
            var context = new AssimpContext();
            var descriptions = context.GetImporterDescriptions();

            Assert.That(descriptions, Is.Not.Null);
            Assert.That(descriptions.Length, Is.GreaterThan(0));

            var descriptionForObj = context.GetImporterDescriptionFor("obj");
            var descriptionForDotObj = context.GetImporterDescriptionFor(".obj");
            Assert.Multiple(() =>
            {
                Assert.That(descriptionForObj, Is.Not.Null);
                Assert.That(descriptionForDotObj, Is.Not.Null);
                Assert.That(descriptionForObj.Name, Is.EqualTo(descriptionForDotObj.Name));
                Assert.That(descriptionForObj, Is.EqualTo(descriptionForDotObj));
            });
        }

        [Test, Parallelizable(ParallelScope.Self)]
        public void TestSupportedFormats()
        {
            var context = new AssimpContext();
            var exportFormatDescriptions = context.GetSupportedExportFormats();
            var importFormats = context.GetSupportedImportFormats();
            Assert.Multiple(() =>
            {
                Assert.That(exportFormatDescriptions, Is.Not.Null);
                Assert.That(importFormats, Is.Not.Null);
                Assert.That(exportFormatDescriptions, Is.Not.Empty);
                Assert.That(importFormats, Is.Not.Empty);
            });
            
            Console.WriteLine("Export formats: ");
            foreach (var description in exportFormatDescriptions)
            {
                Assert.Multiple(() =>
                {
                    Assert.That(description, Is.Not.Null);
                    Assert.That(description.FileExtension, Is.Not.Empty);
                    Assert.That(description.Description, Is.Not.Empty);
                    Assert.That(description.FormatId, Is.Not.Empty);
                });
                Console.WriteLine($"{description.FormatId}: {description.Description} ({description.FileExtension})");
                Assert.That(context.IsExportFormatSupported(description.FileExtension), Is.True);
            }

            Console.WriteLine("Import formats: ");
            foreach (var format in importFormats)
            {
                Assert.That(format, Is.Not.Empty);
                Console.Write($"{format} ");
                Assert.That(context.IsImportFormatSupported(format), Is.True);
            }

            Assert.Multiple(() =>
            {
                Assert.That(context.IsExportFormatSupported("obj"), Is.True);
                Assert.That(context.IsExportFormatSupported(".obj"), Is.True);
            });
            Assert.Multiple(() =>
            {
                Assert.That(context.IsImportFormatSupported("obj"), Is.True);
                Assert.That(context.IsImportFormatSupported(".obj"), Is.True);
            });
        }

        [Test, Parallelizable(ParallelScope.Self)]
        public void TestConvertFromFile()
        {
            var inputPath = Path.Combine(TestHelper.RootPath, "TestFiles/Bob.md5mesh");
            var outputPath = Path.Combine(TestHelper.RootPath, "TestFiles/output/Bob.dae");

            var context = new AssimpContext();
            context.ConvertFromFileToFile(inputPath, outputPath, "collada");

            var blob = context.ConvertFromFileToBlob(inputPath, "collada");
            Assert.That(blob.HasData, Is.True);
        }

        [Test, Parallelizable(ParallelScope.Self)]
        public void TestConvertFromStreamNoFormatHint()
        {
            var logStream = new TestContextLogStream();
            logStream.Attach();
            
            var inputPath = Path.Combine(TestHelper.RootPath, "TestFiles/duck.dae");
            var outputPath = Path.Combine(TestHelper.RootPath, "TestFiles/output/duckNoHint.obj");

            if (File.Exists(outputPath))
                File.Delete(outputPath);
            
            var context = new AssimpContext();
            
            var fs = File.OpenRead(inputPath);
            var success = context.ConvertFromStreamToFile(fs, ".dae", outputPath, "obj");
            fs.Close();
            Assert.Multiple(() =>
            {
                Assert.That(success, Is.True);
                Assert.That(File.Exists(outputPath), Is.True);
                var fileInfo = new FileInfo(outputPath);
                Assert.That(fileInfo.Length, Is.GreaterThan(0));
            });

            logStream.Detach();
        }

        [Test, Parallelizable(ParallelScope.Self)]
        public void TestConvertFromStream()
        {
            var logStream = new TestContextLogStream();
            logStream.Attach();
            
            var inputPath = Path.Combine(TestHelper.RootPath, "TestFiles/duck.dae");
            var outputPath = Path.Combine(TestHelper.RootPath, "TestFiles/output/duck.obj");
            var outputPath2 = Path.Combine(TestHelper.RootPath, "TestFiles/output/duck-fromBlob.obj");
            
            var context = new AssimpContext();

            var fs = File.OpenRead(inputPath);
            var success = context.ConvertFromStreamToFile(fs, ".dae", outputPath, "obj");
            Assert.That(success, Is.True);
            Assert.That(File.Exists(outputPath), Is.True);
            var fileInfo = new FileInfo(outputPath);
            Assert.That(fileInfo.Length, Is.GreaterThan(0));

            fs.Position = 0;
            var blob = context.ConvertFromStreamToBlob(fs, ".dae", "collada");
            fs.Close();
            Assert.Multiple(() =>
            {
                Assert.That(blob.HasData, Is.True);
                Assert.That(blob.Data, Is.Not.Empty);
            });

            //Take ExportDataBlob's data, write it to a memory stream and export that back to an obj and write it
            var memStream = new MemoryStream();
            memStream.Write(blob.Data, 0, blob.Data.Length);
            memStream.Position = 0;
            success = context.ConvertFromStreamToFile(memStream, ".dae", outputPath2, "obj");
            memStream.Close();
            Assert.Multiple(() =>
            {
                Assert.That(success, Is.True);
                Assert.That(File.Exists(outputPath2), Is.True);
                fileInfo = new FileInfo(outputPath2);
                Assert.That(fileInfo.Length, Is.GreaterThan(0));
            });

            logStream.Detach();
        }

        [Test, Parallelizable(ParallelScope.None)]
        public void TestMultipleFileImportersMultipleThreads([Range(4,132, 16)]int threadCount)
        {
            var rng = new Random(threadCount);

            LogStream.IsVerboseLoggingEnabled = true;

            var threads = new List<Thread>(threadCount);
            for (var i = 0; i < threadCount; ++i) {
                threads.Add((i % 4) switch {
                    0 => new Thread(LoadSceneA),
                    1 => new Thread(LoadSceneB),
                    2 => new Thread(ConvertSceneC),
                    3 => new Thread(ConvertSceneD),
                    _ => throw new ArgumentOutOfRangeException()
                });
            }

            threads.Shuffle(rng);
            
            for (var i = 0; i < threadCount; ++i)
                threads[i].Start(new TestContextLogStream());
            
            threads.Shuffle(rng);

            for (var i = 0; i < threadCount; ++i)
                threads[i].Join();
        }

        [Test, Parallelizable(ParallelScope.None)]
        public void TestMultipleStreamingImportersMultipleThreads([Range(4,132, 16)]int threadCount) {
            var rng = new Random(threadCount);
            
            LogStream.IsVerboseLoggingEnabled = true;

            var threads = new List<Thread>(threadCount);
            for (var i = 0; i < threadCount; ++i) {
                threads.Add((i % 4) switch {
                    0 => new Thread(StreamSceneE),
                    1 => new Thread(StreamSceneF),
                    2 => new Thread(ConvertStreamSceneG),
                    3 => new Thread(ConvertStreamSceneH),
                    _ => throw new ArgumentOutOfRangeException()
                });
            }

            threads.Shuffle(rng);
            
            for (var i = 0; i < threadCount; ++i)
                threads[i].Start(new TestContextLogStream());
            
            threads.Shuffle(rng);

            for (var i = 0; i < threadCount; ++i)
                threads[i].Join();
        }

        [Test, Parallelizable(ParallelScope.None)]
        public void TestMultipleImportersMultipleThreads([Range(8,136, 16)]int threadCount) {
            var rng = new Random(threadCount);
            
            LogStream.IsVerboseLoggingEnabled = true;

            var threads = new List<Thread>(threadCount);

            for (var i = 0; i < threadCount; ++i) {
                threads.Add((i % 8) switch {
                    0 => new Thread(LoadSceneA),
                    1 => new Thread(LoadSceneB),
                    2 => new Thread(ConvertSceneC),
                    3 => new Thread(ConvertSceneD),
                    4 => new Thread(StreamSceneE),
                    5 => new Thread(StreamSceneF),
                    6 => new Thread(ConvertStreamSceneG),
                    7 => new Thread(ConvertStreamSceneH),
                    _ => throw new ArgumentOutOfRangeException()
                });
            }

            threads.Shuffle(rng);
            
            for (var i = 0; i < threadCount; ++i)
                threads[i].Start(new TestContextLogStream());
            
            threads.Shuffle(rng);

            for (var i = 0; i < threadCount; ++i)
                threads[i].Join();
        }

        private void LoadSceneA(object logStreamObj)
        {
            var logStream = (TestContextLogStream)logStreamObj;
            logStream.UserData = "Thread A";
            logStream.Attach();
            
            logStream.Log("Establishing Context for import");
            var context = new AssimpContext();
            var path = Path.Combine(TestHelper.RootPath, "TestFiles/Bob.md5mesh");

            logStream.Log("Importing");
            var scene = context.ImportFile(path);
            Assert.Multiple(() =>
            {
                Assert.That(scene, Is.Not.Null);
                Assert.That(scene.SceneFlags & SceneFlags.Incomplete, Is.Not.EqualTo(SceneFlags.Incomplete));
            });
            logStream.Log("Done importing");
            
            logStream.Detach();
        }

        private void LoadSceneB(object logStreamObj)
        {
            var logStream = (TestContextLogStream)logStreamObj;
            logStream.UserData = "Thread B";
            logStream.Attach();

            logStream.Log("Establishing Context for import");
            var context = new AssimpContext();
            var path = Path.Combine(TestHelper.RootPath, "TestFiles/duck.dae");

            context.SetConfig(new NormalSmoothingAngleConfig(55.0f));
            logStream.Log("Importing");
            var scene = context.ImportFile(path);
            Assert.Multiple(() =>
            {
                Assert.That(scene, Is.Not.Null);
                Assert.That(scene.SceneFlags & SceneFlags.Incomplete, Is.Not.EqualTo(SceneFlags.Incomplete));
            });
            logStream.Log("Done importing");

            logStream.Detach();
        }

        private void ConvertSceneC(object logStreamObj)
        {
            var logStream = (TestContextLogStream)logStreamObj;
            logStream.UserData = "Thread C";
            logStream.Attach();
            
            logStream.Log("Establishing Context for conversionEstablishing Context for conversion");
            var context = new AssimpContext();
            var path = Path.Combine(TestHelper.RootPath, "TestFiles/duck.dae");

            context.SetConfig(new NormalSmoothingAngleConfig(55.0f));
            context.SetConfig(new FavorSpeedConfig(true));

            logStream.Log("Converting");
            var blob = context.ConvertFromFileToBlob(path, "obj");
            Assert.Multiple(() =>
            {
                Assert.That(blob.HasData, Is.True);
                Assert.That(blob.Data, Is.Not.Empty);
            });
            logStream.Log("Done converting");
            
            logStream.Detach();
        }

        private void ConvertSceneD(object logStreamObj)
        {
            var logStream = (TestContextLogStream)logStreamObj;
            logStream.UserData = "Thread D";
            logStream.Attach();
            
            logStream.Log("Establishing Context for conversion");
            var context = new AssimpContext();
            var path = Path.Combine(TestHelper.RootPath, "TestFiles/Bob.md5mesh");

            context.SetConfig(new NormalSmoothingAngleConfig(55.0f));
            context.SetConfig(new FavorSpeedConfig(true));

            logStream.Log("Converting");
            var blob = context.ConvertFromFileToBlob(path, "obj");
            Assert.Multiple(() =>
            {
                Assert.That(blob.HasData, Is.True);
                Assert.That(blob.Data, Is.Not.Empty);
            });
            logStream.Log("Done converting");
            
            logStream.Detach();
        }
        
        private void StreamSceneE(object logStreamObj)
        {
            var logStream = (TestContextLogStream)logStreamObj;
            logStream.UserData = "Thread E";
            logStream.Attach();

            logStream.Log("Establishing Context for stream import");
            var context = new AssimpContext();
            
            logStream.Log("Importing");
            var path = Path.Combine(TestHelper.RootPath, "TestFiles/Bob.md5mesh");

            using var sr = new StreamReader(path);
            var streamScene = context.ImportFileFromStream(sr.BaseStream);
            sr.Close();
            Assert.Multiple(() =>
            {
                Assert.That(streamScene, Is.Not.Null);
                Assert.That(streamScene.SceneFlags & SceneFlags.Incomplete, Is.Not.EqualTo(SceneFlags.Incomplete));
            });
            logStream.Log("Done importing");
            
            logStream.Detach();
        }

        private void StreamSceneF(object logStreamObj)
        {
            var logStream = (TestContextLogStream)logStreamObj;
            logStream.UserData = "Thread F";
            logStream.Attach();
            
            logStream.Log("Establishing Context for Stream import");
            var context = new AssimpContext();
            
            logStream.Log("Importing");
            var path = Path.Combine(TestHelper.RootPath, "TestFiles/duck.dae");

            using var sr = new StreamReader(path);
            var streamScene = context.ImportFileFromStream(sr.BaseStream);
            sr.Close();
            
            Assert.Multiple(() =>
            {
                Assert.That(streamScene, Is.Not.Null);
                Assert.That(streamScene.SceneFlags & SceneFlags.Incomplete, Is.Not.EqualTo(SceneFlags.Incomplete));
            });
            logStream.Log("Done importing");
            
            logStream.Detach();
        }

        private void ConvertStreamSceneG(object logStreamObj)
        {
            var logStream = (TestContextLogStream)logStreamObj;
            logStream.UserData = "Thread G";
            logStream.Attach();
            
            logStream.Log("Establishing Context for Stream conversion");
            var importer = new AssimpContext();
            var inputPath = Path.Combine(TestHelper.RootPath, "TestFiles/duck.dae");
            var inputHint = Path.GetExtension(inputPath).TrimStart('.');
            var outputPath = Path.GetTempFileName();
            const string outputHint = "obj";
            
            importer.SetConfig(new NormalSmoothingAngleConfig(55.0f));
            importer.SetConfig(new FavorSpeedConfig(true));

            logStream.Log("Converting");
            using var sr = new StreamReader(inputPath);
            var success = importer.ConvertFromStreamToFile(sr.BaseStream, inputHint, outputPath, outputHint);
            sr.Close();
            Assert.Multiple(() =>
            {
                Assert.That(success, Is.True);
                Assert.That(File.Exists(outputPath), Is.True);
                var fileInfo = new FileInfo(outputPath);
                Assert.That(fileInfo.Length, Is.GreaterThan(0));
            });
            logStream.Log("Done converting");
            
            logStream.Detach();
        }

        private void ConvertStreamSceneH(object logStreamObj)
        {
            var logStream = (TestContextLogStream)logStreamObj;
            logStream.UserData = "Thread H";
            logStream.Attach();
            
            logStream.Log("Establishing Context for Stream conversion");
            var importer = new AssimpContext();
            var inputPath = Path.Combine(TestHelper.RootPath, "TestFiles/Bob.md5mesh");
            var inputHint = Path.GetExtension(inputPath).TrimStart('.');
            var outputPath = Path.GetTempFileName();
            const string outputHint = "obj";
            
            importer.SetConfig(new NormalSmoothingAngleConfig(55.0f));
            importer.SetConfig(new FavorSpeedConfig(true));

            logStream.Log("Converting");
            using var sr = new StreamReader(inputPath);
            var success = importer.ConvertFromStreamToFile(sr.BaseStream, inputHint, outputPath, outputHint);
            sr.Close();
            Assert.Multiple(() =>
            {
                Assert.That(success, Is.True);
                Assert.That(File.Exists(outputPath), Is.True);
                var fileInfo = new FileInfo(outputPath);
                Assert.That(fileInfo.Length, Is.GreaterThan(0));
            });
            logStream.Log("Done converting");
            
            logStream.Detach();
        }
    }
}
