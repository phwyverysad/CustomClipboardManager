import os

file_path = "MainWindow.xaml"
with open(file_path, 'r', encoding='utf-8') as f:
    content = f.read()

replacements = {
    'Background="#D91E1E1E"': 'Background="{DynamicResource AppBackgroundBrush}"',
    'Background="#1AFFFFFF"': 'Background="{DynamicResource ElementBackgroundBrush}"',
    'Background="#33FFFFFF"': 'Background="{DynamicResource ElementHoverBrush}"',
    'Background="#4DFFFFFF"': 'Background="{DynamicResource ElementPressedBrush}"',
    'BorderBrush="#22FFFFFF"': 'BorderBrush="{DynamicResource AppBorderBrush}"',
    'Foreground="#88FFFFFF"': 'Foreground="{DynamicResource TextMutedBrush}"',
    'Foreground="#66FFFFFF"': 'Foreground="{DynamicResource TextMutedBrush}"',
    'Foreground="#44FFFFFF"': 'Foreground="{DynamicResource TextFaintBrush}"',
    'Foreground="#22FFFFFF"': 'Foreground="{DynamicResource TextFaintBrush}"',
    'Foreground="White"': 'Foreground="{DynamicResource TextForegroundBrush}"',
    'CaretBrush="White"': 'CaretBrush="{DynamicResource TextForegroundBrush}"'
}

for k, v in replacements.items():
    content = content.replace(k, v)

with open(file_path, 'w', encoding='utf-8') as f:
    f.write(content)

print("Done replacing.")
