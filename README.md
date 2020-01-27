# Connect to the cluster

We use the cluster service offered by Pachyderm:

- In the Pachyderm Hub UI, click Connect next to your cluster.
- In your terminal window, copy, paste, and run the commands listed in the instructions

We check that pachctl is installed properly:

```sh
pachctl version --client-only

1.9.11
```

```sh
pachctl config get active-context

miCluster-romzdt8oa5
```

We create a repo for our data:

```sh
pachctl create repo images

pachctl list repo

NAME   CREATED            SIZE (MASTER) ACCESS LEVEL
images 7 seconds ago      0B            OWNER
test   About a minute ago 0B            OWNER
```

We add an image to the repo, in the `master` branch:

```sh
pachctl put file images@master:liberty.png -f http://imgur.com/46Q8nDz.png

pachctl list repo

NAME   CREATED            SIZE (MASTER) ACCESS LEVEL
images 31 seconds ago     57.27KiB      OWNER
test   About a minute ago 0B            OWNER
```

The addition is shown as a commit in the repo:

```sh
pachctl list commit images

REPO   BRANCH COMMIT                           FINISHED       SIZE     PROGRESS DESCRIPTION
images master 8c6716c74f754b418d4fe8a513d45b20 24 seconds ago 57.27KiB -
```

We can also see the content in the repo:

```sh
pachctl list file images@master

NAME         TYPE SIZE
/liberty.png file 57.27KiB

pachctl get file images@master:liberty.png | display
```

We prepare a pipeline definition. The image will use the `images` repo as input:

```json
# edges.json
{
  "pipeline": {
    "name": "edges"
  },
  "description": "A pipeline that performs image edge detection by using the OpenCV library.",
  "transform": {
    "cmd": [ "python3", "/edges.py" ],
    "image": "pachyderm/opencv"
  },
  "input": {
    "pfs": {
      "repo": "images",
      "glob": "/*"
    }
  }
}
```

We create the pipeline. As soon it is created a job is automatically triggered to process the data in the input repo:

```sh
pachctl create pipeline -f https://raw.githubusercontent.com/pachyderm/pachyderm/master/examples/opencv/edges.json

pachctl list job

ID                               PIPELINE STARTED            DURATION  RESTART PROGRESS  DL       UL       STATE
1b62f467b6874c778212dc13b6371cf3 edges    About a minute ago 2 seconds 0       1 + 0 / 1 57.27KiB 22.22KiB success
```

When the jobs finishes we will have a new repo, the output repo. The repo has the same name as the pipeline:

```sh
pachctl list repo

NAME   CREATED       SIZE (MASTER) ACCESS LEVEL
edges  2 minutes ago 22.22KiB      OWNER        Output repo for pipeline edges.
images 5 minutes ago 57.27KiB      OWNER
test   6 minutes ago 0B            OWNER

pachctl get file edges@master:liberty.png | display
```

If we add a couple of images the pipeline is triggered again. In this case we are commiting after writing each image, so two jobs will be run:

```sh
pachctl put file images@master:AT-AT.png -f http://imgur.com/8MN9Kg0.png

pachctl put file images@master:kitten.png -f http://imgur.com/g2QnNqa.png

pachctl list job

ID                               PIPELINE STARTED        DURATION  RESTART PROGRESS  DL       UL       STATE
480bc659037045cf869c5bacb9a4774d edges    5 seconds ago  3 seconds 0       1 + 2 / 3 102.4KiB 74.21KiB success
58591e7f4b154761a97c20f0c3b3e8be edges    15 seconds ago 2 seconds 0       1 + 1 / 2 78.7KiB  37.15KiB success
1b62f467b6874c778212dc13b6371cf3 edges    2 minutes ago  2 seconds 0       1 + 0 / 1 57.27KiB 22.22KiB success
```

We should see the output in the `edges` repo:

```sh
pachctl get file edges@master:AT-AT.png | display

pachctl get file edges@master:kitten.png | display
```

We prepara another pipeline. This one uses the `images` and `edges` repos as input, using the `cross` operator:

```json
# montage.json
{
  "pipeline": {
    "name": "montage"
  },
  "description": "A pipeline that combines images from the `images` and `edges` repositories into a montage.",
  "input": {
    "cross": [ {
      "pfs": {
        "glob": "/",
        "repo": "images"
      }
    },
    {
      "pfs": {
        "glob": "/",
        "repo": "edges"
      }
    } ]
  },
  "transform": {
    "cmd": [ "sh" ],
    "image": "v4tech/imagemagick",
    "stdin": [ "montage -shadow -background SkyBlue -geometry 300x300+2+2 $(find /pfs -type f | sort) /pfs/out/montage.png" ]
  }
}
```

We create the pipeline. It will trigger the job automatically:

```sh
pachctl create pipeline -f https://raw.githubusercontent.com/pachyderm/pachyderm/master/examples/opencv/montage.json

pachctl list job

ID                               PIPELINE STARTED            DURATION  RESTART PROGRESS  DL       UL       STATE
0be1bb438e6142e69b12622eb033a563 montage  20 seconds ago     4 seconds 0       1 + 0 / 1 371.9KiB 1.284MiB success
480bc659037045cf869c5bacb9a4774d edges    About a minute ago 3 seconds 0       1 + 2 / 3 102.4KiB 74.21KiB success
58591e7f4b154761a97c20f0c3b3e8be edges    About a minute ago 2 seconds 0       1 + 1 / 2 78.7KiB  37.15KiB success
1b62f467b6874c778212dc13b6371cf3 edges    3 minutes ago      2 seconds 0       1 + 0 / 1 57.27KiB 22.22KiB success
```

We can see how this new pipeline has created a new output repo:

```sh
pachctl list repo

NAME    CREATED            SIZE (MASTER) ACCESS LEVEL
montage About a minute ago 1.284MiB      OWNER        Output repo for pipeline montage.
edges   4 minutes ago      133.6KiB      OWNER        Output repo for pipeline edges.
images  8 minutes ago      238.3KiB      OWNER
test    9 minutes ago      0B            OWNER
```

# Proof of Concept

## Create an image

We have created an docker file with this project. We create the image without specifying any entrypoint or cmd, because that will be set on the pachyderm pipelines.

```yaml
FROM mcr.microsoft.com/dotnet/core/runtime:3.1
COPY ./bin/Release/netcoreapp3.1/ app/
```

We build the image:

```sh
docker build -t clasifica -f .\Dockerfile.txt .
```

We can run the image as:

```sh
docker run -it clasifica dotnet /app/clasifica.dll 5 /tmp /tmp /euge
```

We create add a tag to the image:

```sh
docker tag  clasifica egsmartin/clasifica:latest
```

We push the image to the docker hub:

```sh
docker push egsmartin/clasifica:latest

he push refers to repository [docker.io/egsmartin/clasifica]
d192b6bcae72: Pushed			
fcd021389694: Pushed                          
2c52aed6692d: Pushed
c51868eee26f: Pushed
556c5fb0d91b: Pushed
latest: digest: sha256:82dbe8c29d11c97bc44ab90ece3a5d29b1e07244ddd68e01addff75458f2bbbb size: 1373
```



## Create the cluster

We log in the [Paychderm hub](https://hub.pachyderm.com/clusters) and create a cluster.

We open a linux session as `su`. Then we click connect to fid out the connection details. In my case:

```sh
echo '{"pachd_address": "grpcs://grpc-hub-c0-drn6jasbh2.clusters.pachyderm.io:31400", "source": 2}' | pachctl config set context edades-7lq7ra3t96 && pachctl config set active-context edades-7lq7ra3t96
```

```sh
pachctl auth login --one-time-password
```

With the one time password as:

```sh
otp/b10b8a9162e54018b090afea9afb17d6
```

## Upload the data
```sh
pachctl create repo personas

pachctl list repo

NAME     CREATED        SIZE (MASTER) ACCESS LEVEL
personas 11 seconds ago 0B            OWNER
```

```sh
pachctl put file personas@master:Personas1.txt -f https://raw.githubusercontent.com/eugeniogarcia/pachyderm/master/data/personas/Personas1.txt
pachctl put file personas@master:Personas2.txt -f https://raw.githubusercontent.com/eugeniogarcia/pachyderm/master/data/personas/Personas2.txt
pachctl put file personas@master:Personas3.txt -f https://raw.githubusercontent.com/eugeniogarcia/pachyderm/master/data/personas/Personas3.txt
```

```sh
pachctl list repo

NAME     CREATED        SIZE (MASTER) ACCESS LEVEL
personas 49 seconds ago 190.8KiB      OWNER
```

```sh
pachctl list commit personas

REPO     BRANCH COMMIT                           FINISHED       SIZE     PROGRESS DESCRIPTION
personas master e20f0934729f47508420e3539c30b6cb 17 seconds ago 190.8KiB -      
personas master 0a280660e7244e24b8319559124b9cdf 24 seconds ago 127.2KiB -      
personas master 6072a48c189d4573b1a42526c9a8c712 34 seconds ago 63.59KiB -
```

```sh
pachctl list file personas@master

NAME           TYPE SIZE
/Personas1.txt file 63.59KiB
/Personas2.txt file 63.58KiB
/Personas3.txt file 63.6KiB
```

```sh
pachctl get file personas@master:Personas1.txt

1;Eugenio;Garcia San Martin;V;1969;P;San Pedro Bercianos
2;Vera Carmen;Zach;H;1973;M;Salzburg
```

## Create the pipeline

```sh
pachctl create pipeline -f https://raw.githubusercontent.com/eugeniogarcia/pachyderm/master/pipelines/edades.json
```

```sh
pachctl list job
```

```sh
pachctl list repo
```

```sh
pachctl list commit edades
```

```sh
pachctl list file edades@master
```

```sh
pachctl get file edades@master:Persona1.txt
```


