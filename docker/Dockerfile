# Dockerfile para servidor Godot headless
FROM ubuntu:22.04

# Instalar dependencias y .NET 8.0
RUN apt-get update && apt-get install -y \
    wget \
    unzip \
    ca-certificates \
    libgl1 \
    libglu1-mesa \
    dotnet-sdk-8.0 \
    && rm -rf /var/lib/apt/lists/*

# Descargar Godot Mono 4.3 stable desde GitHub
ENV GODOT_VERSION="4.3"
RUN wget -O godot.zip https://github.com/godotengine/godot-builds/releases/download/4.3-stable/Godot_v4.3-stable_mono_linux_x86_64.zip \
    && unzip godot.zip -d /usr/local/bin/ \
    && rm godot.zip \
    && find /usr/local/bin/Godot_v4.3-stable_mono_linux_x86_64 -name "Godot_*" -type f -executable -exec chmod +x {} \; \
    && ln -s /usr/local/bin/Godot_v4.3-stable_mono_linux_x86_64 /usr/local/bin/godot

# Crear directorio de trabajo
WORKDIR /app

# Exponer puerto del servidor
EXPOSE 7777

# Ejecutar el binario de Godot
CMD ["/usr/local/bin/godot/Godot_v4.3-stable_mono_linux.x86_64", "--headless", "--path", "/app", "--", "--server", "--port", "7777"]
