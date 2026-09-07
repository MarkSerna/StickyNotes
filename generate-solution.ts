import fs from 'fs';
import path from 'path';
import {
  SLN_CONTENT,
  CSPROJ_CORE,
  CSPROJ_DATA,
  CSPROJ_SYNC,
  CSPROJ_APP,
  APP_MANIFEST,
  APP_XAML,
  APP_XAML_CS
} from './src/utils/solutionExporter.ts';
import {
  GENERATED_STEP2_FILES,
  GENERATED_STEP3_FILES,
  GENERATED_STEP4_FILES,
  GENERATED_STEP5_FILES,
  GENERATED_STEP6_FILES,
  GENERATED_STEP7_FILES
} from './src/components/CodeGeneratedView.tsx';

const rootDir = process.cwd();

// 1. Solution file
fs.writeFileSync(path.join(rootDir, 'StickyNotes.sln'), SLN_CONTENT, 'utf8');
console.log('StickyNotes.sln written');

// 2. Project csproj files
const projects = [
  { dir: 'StickyNotes.Core', file: 'StickyNotes.Core.csproj', content: CSPROJ_CORE },
  { dir: 'StickyNotes.Data', file: 'StickyNotes.Data.csproj', content: CSPROJ_DATA },
  { dir: 'StickyNotes.Sync', file: 'StickyNotes.Sync.csproj', content: CSPROJ_SYNC },
  { dir: 'StickyNotes.App', file: 'StickyNotes.App.csproj', content: CSPROJ_APP },
  { dir: 'StickyNotes.App', file: 'app.manifest', content: APP_MANIFEST },
  { dir: 'StickyNotes.App', file: 'App.xaml', content: APP_XAML },
  { dir: 'StickyNotes.App', file: 'App.xaml.cs', content: APP_XAML_CS },
];

for (const p of projects) {
  const targetDir = path.join(rootDir, p.dir);
  if (!fs.existsSync(targetDir)) {
    fs.mkdirSync(targetDir, { recursive: true });
  }
  fs.writeFileSync(path.join(targetDir, p.file), p.content, 'utf8');
  console.log(`Written: ${p.dir}/${p.file}`);
}

// 3. All generated C# files
const allFiles = [
  ...GENERATED_STEP2_FILES,
  ...GENERATED_STEP3_FILES,
  ...GENERATED_STEP4_FILES,
  ...GENERATED_STEP5_FILES,
  ...GENERATED_STEP6_FILES,
  ...GENERATED_STEP7_FILES
];

for (const f of allFiles) {
  const targetPath = path.join(rootDir, f.path);
  const parentDir = path.dirname(targetPath);
  if (!fs.existsSync(parentDir)) {
    fs.mkdirSync(parentDir, { recursive: true });
  }
  fs.writeFileSync(targetPath, f.content, 'utf8');
  console.log(`Written: ${f.path}`);
}

console.log('ALL C# & WINUI 3 SOLUTION FILES SUCCESSFULLY GENERATED!');
