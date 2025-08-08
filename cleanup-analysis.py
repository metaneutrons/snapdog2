#!/usr/bin/env python3
"""
SnapDog2 Code Cleanup Analysis Tool

This tool analyzes the codebase to identify:
1. Unused/deprecated files that can be safely removed
2. Unreferenced classes and methods
3. Duplicate or superseded implementations
4. Temporary files and scripts

Usage: python3 cleanup-analysis.py [--dry-run] [--interactive]
"""

import os
import re
import sys
from pathlib import Path
from typing import List, Dict, Set, Tuple
import argparse

class CodeCleanupAnalyzer:
    def __init__(self, project_root: str):
        self.project_root = Path(project_root)
        self.cs_files = []
        self.references = {}
        self.deprecated_files = []
        self.unused_files = []
        self.temp_files = []
        
    def scan_codebase(self):
        """Scan the entire codebase for C# files and build reference map."""
        print("🔍 Scanning codebase...")
        
        # Find all C# files
        for cs_file in self.project_root.rglob("*.cs"):
            if self._should_analyze_file(cs_file):
                self.cs_files.append(cs_file)
        
        print(f"Found {len(self.cs_files)} C# files to analyze")
        
        # Build reference map
        self._build_reference_map()
        
    def _should_analyze_file(self, file_path: Path) -> bool:
        """Determine if a file should be analyzed."""
        exclude_patterns = [
            "bin/", "obj/", ".git/", "packages/",
            "AssemblyInfo.cs", "GlobalUsings.cs"
        ]
        
        file_str = str(file_path)
        return not any(pattern in file_str for pattern in exclude_patterns)
    
    def _build_reference_map(self):
        """Build a map of class/interface references across the codebase."""
        print("🔗 Building reference map...")
        
        # Extract class/interface definitions and their usages
        for cs_file in self.cs_files:
            try:
                with open(cs_file, 'r', encoding='utf-8') as f:
                    content = f.read()
                    
                # Find class/interface definitions
                definitions = self._extract_definitions(content, cs_file)
                
                # Find references to other classes
                references = self._extract_references(content, cs_file)
                
                self.references[cs_file] = {
                    'definitions': definitions,
                    'references': references
                }
                
            except Exception as e:
                print(f"⚠️  Error analyzing {cs_file}: {e}")
    
    def _extract_definitions(self, content: str, file_path: Path) -> List[str]:
        """Extract class, interface, record, and enum definitions."""
        definitions = []
        
        patterns = [
            r'public\s+(?:partial\s+)?class\s+(\w+)',
            r'public\s+(?:partial\s+)?interface\s+(\w+)',
            r'public\s+(?:partial\s+)?record\s+(\w+)',
            r'public\s+(?:partial\s+)?enum\s+(\w+)',
            r'internal\s+(?:partial\s+)?class\s+(\w+)',
            r'internal\s+(?:partial\s+)?interface\s+(\w+)',
        ]
        
        for pattern in patterns:
            matches = re.findall(pattern, content, re.MULTILINE)
            definitions.extend(matches)
            
        return definitions
    
    def _extract_references(self, content: str, file_path: Path) -> Set[str]:
        """Extract references to other classes/interfaces."""
        references = set()
        
        # Remove comments and strings to avoid false positives
        content_clean = re.sub(r'//.*$', '', content, flags=re.MULTILINE)
        content_clean = re.sub(r'/\*.*?\*/', '', content_clean, flags=re.DOTALL)
        content_clean = re.sub(r'"[^"]*"', '', content_clean)
        
        # Find type references
        patterns = [
            r':\s*(\w+)',  # Inheritance
            r'<(\w+)>',    # Generic types
            r'(\w+)\s+\w+\s*[=;]',  # Variable declarations
            r'new\s+(\w+)\s*\(',    # Constructor calls
            r'typeof\s*\(\s*(\w+)',  # typeof expressions
            r'(\w+)\.', # Static member access
        ]
        
        for pattern in patterns:
            matches = re.findall(pattern, content_clean)
            references.update(matches)
            
        return references
    
    def identify_deprecated_files(self):
        """Identify files that are explicitly deprecated or superseded."""
        print("🗑️  Identifying deprecated files...")
        
        deprecated_candidates = [
            # Old consolidated command files (superseded by individual files)
            "ZoneCommands.cs",
            "ClientVolumeCommands.cs", 
            "ClientConfigCommands.cs",
            
            # Old behavior files (superseded by shared behaviors)
            "LoggingCommandBehavior.cs",
            "LoggingQueryBehavior.cs",
        ]
        
        for cs_file in self.cs_files:
            file_name = cs_file.name
            
            # Check if it's a known deprecated file
            if file_name in deprecated_candidates:
                self.deprecated_files.append({
                    'file': cs_file,
                    'reason': 'Superseded by new implementation',
                    'replacement': self._get_replacement_info(file_name)
                })
                continue
                
            # Check file content for deprecation markers
            try:
                with open(cs_file, 'r', encoding='utf-8') as f:
                    content = f.read()
                    
                if any(marker in content.lower() for marker in ['deprecated', 'obsolete', 'todo: remove']):
                    self.deprecated_files.append({
                        'file': cs_file,
                        'reason': 'Marked as deprecated in code',
                        'replacement': 'See code comments'
                    })
                    
            except Exception as e:
                print(f"⚠️  Error reading {cs_file}: {e}")
    
    def _get_replacement_info(self, file_name: str) -> str:
        """Get replacement information for deprecated files."""
        replacements = {
            "ZoneCommands.cs": "Individual files in Commands/Playback/, Commands/Volume/, etc.",
            "ClientVolumeCommands.cs": "Individual files in Commands/Volume/",
            "ClientConfigCommands.cs": "Individual files in Commands/Config/",
            "LoggingCommandBehavior.cs": "SharedLoggingCommandBehavior.cs",
            "LoggingQueryBehavior.cs": "SharedLoggingQueryBehavior.cs",
        }
        return replacements.get(file_name, "Unknown")
    
    def identify_unused_files(self):
        """Identify files that appear to be unused based on reference analysis."""
        print("🔍 Identifying potentially unused files...")
        
        all_definitions = {}
        all_references = set()
        
        # Collect all definitions and references
        for file_path, data in self.references.items():
            for definition in data['definitions']:
                all_definitions[definition] = file_path
            all_references.update(data['references'])
        
        # Find definitions that are never referenced
        for definition, file_path in all_definitions.items():
            if definition not in all_references:
                # Skip if it's a known entry point or special class
                if not self._is_entry_point_class(definition, file_path):
                    self.unused_files.append({
                        'file': file_path,
                        'class': definition,
                        'reason': 'Class appears to be unreferenced'
                    })
    
    def _is_entry_point_class(self, class_name: str, file_path: Path) -> bool:
        """Check if a class is an entry point that might not have explicit references."""
        entry_point_patterns = [
            r'Controller$',  # API Controllers
            r'Handler$',     # Command/Query handlers (registered via reflection)
            r'Service$',     # Services (registered via DI)
            r'Configuration$', # Configuration classes
            r'Program$',     # Program entry point
            r'Startup$',     # Startup class
        ]
        
        return any(re.search(pattern, class_name) for pattern in entry_point_patterns)
    
    def identify_temp_files(self):
        """Identify temporary files and scripts."""
        print("🧹 Identifying temporary files...")
        
        temp_patterns = [
            "*.tmp", "*.temp", "*.bak", "*.old",
            "generate_*.py", "fix_*.py", "cleanup_*.py",
            "test_*.py", "debug_*.py"
        ]
        
        for pattern in temp_patterns:
            for temp_file in self.project_root.rglob(pattern):
                if temp_file.is_file():
                    self.temp_files.append({
                        'file': temp_file,
                        'reason': f'Temporary file matching pattern: {pattern}'
                    })
    
    def generate_report(self) -> str:
        """Generate a comprehensive cleanup report."""
        report = []
        report.append("# SnapDog2 Code Cleanup Analysis Report")
        report.append("=" * 50)
        report.append("")
        
        # Deprecated files section
        if self.deprecated_files:
            report.append("## 🗑️ Deprecated Files (Safe to Remove)")
            report.append("")
            for item in self.deprecated_files:
                report.append(f"**File**: `{item['file'].relative_to(self.project_root)}`")
                report.append(f"**Reason**: {item['reason']}")
                report.append(f"**Replacement**: {item['replacement']}")
                report.append("")
        
        # Unused files section
        if self.unused_files:
            report.append("## 🔍 Potentially Unused Files (Review Required)")
            report.append("")
            for item in self.unused_files:
                report.append(f"**File**: `{item['file'].relative_to(self.project_root)}`")
                report.append(f"**Class**: {item['class']}")
                report.append(f"**Reason**: {item['reason']}")
                report.append("")
        
        # Temporary files section
        if self.temp_files:
            report.append("## 🧹 Temporary Files (Safe to Remove)")
            report.append("")
            for item in self.temp_files:
                report.append(f"**File**: `{item['file'].relative_to(self.project_root)}`")
                report.append(f"**Reason**: {item['reason']}")
                report.append("")
        
        # Summary
        report.append("## 📊 Summary")
        report.append("")
        report.append(f"- **Deprecated files**: {len(self.deprecated_files)}")
        report.append(f"- **Potentially unused files**: {len(self.unused_files)}")
        report.append(f"- **Temporary files**: {len(self.temp_files)}")
        report.append(f"- **Total files analyzed**: {len(self.cs_files)}")
        report.append("")
        
        return "\n".join(report)
    
    def generate_cleanup_script(self) -> str:
        """Generate a bash script to perform the cleanup."""
        script = []
        script.append("#!/bin/bash")
        script.append("# SnapDog2 Automated Cleanup Script")
        script.append("# Generated by cleanup-analysis.py")
        script.append("")
        script.append("set -e")
        script.append("")
        script.append("echo '🧹 Starting SnapDog2 cleanup...'")
        script.append("")
        
        # Remove deprecated files
        if self.deprecated_files:
            script.append("echo '🗑️  Removing deprecated files...'")
            for item in self.deprecated_files:
                rel_path = item['file'].relative_to(self.project_root)
                script.append(f"echo 'Removing {rel_path} (superseded)'")
                script.append(f"rm -f '{rel_path}'")
            script.append("")
        
        # Remove temporary files
        if self.temp_files:
            script.append("echo '🧹 Removing temporary files...'")
            for item in self.temp_files:
                rel_path = item['file'].relative_to(self.project_root)
                script.append(f"echo 'Removing {rel_path} (temporary)'")
                script.append(f"rm -f '{rel_path}'")
            script.append("")
        
        script.append("echo '✅ Cleanup completed!'")
        script.append("echo 'Running build to verify...'")
        script.append("dotnet build")
        script.append("echo '✅ Build successful - cleanup verified!'")
        
        return "\n".join(script)

def main():
    parser = argparse.ArgumentParser(description='Analyze SnapDog2 codebase for cleanup opportunities')
    parser.add_argument('--dry-run', action='store_true', help='Generate report only, do not create cleanup script')
    parser.add_argument('--interactive', action='store_true', help='Interactive mode for reviewing findings')
    parser.add_argument('--project-root', default='.', help='Project root directory')
    
    args = parser.parse_args()
    
    analyzer = CodeCleanupAnalyzer(args.project_root)
    
    # Perform analysis
    analyzer.scan_codebase()
    analyzer.identify_deprecated_files()
    analyzer.identify_unused_files()
    analyzer.identify_temp_files()
    
    # Generate report
    report = analyzer.generate_report()
    
    # Save report
    report_file = Path(args.project_root) / "CLEANUP_ANALYSIS.md"
    with open(report_file, 'w') as f:
        f.write(report)
    
    print(f"📋 Analysis complete! Report saved to: {report_file}")
    print()
    print(report)
    
    if not args.dry_run:
        # Generate cleanup script
        script = analyzer.generate_cleanup_script()
        script_file = Path(args.project_root) / "cleanup.sh"
        
        with open(script_file, 'w') as f:
            f.write(script)
        
        # Make script executable
        os.chmod(script_file, 0o755)
        
        print(f"🔧 Cleanup script generated: {script_file}")
        print("Run './cleanup.sh' to perform automated cleanup")
        
        if args.interactive:
            response = input("\n❓ Would you like to run the cleanup now? (y/N): ")
            if response.lower() == 'y':
                os.system(f"cd {args.project_root} && ./cleanup.sh")

if __name__ == "__main__":
    main()
