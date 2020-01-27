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
pachctl put file personas@master:Personas1.txt -f https://raw.githubusercontent.com/eugeniogarcia/pachyderm/master/data/personas/Personas1.txt
pachctl put file personas@master:Personas2.txt -f https://raw.githubusercontent.com/eugeniogarcia/pachyderm/master/data/personas/Personas2.txt
pachctl put file personas@master:Personas3.txt -f https://raw.githubusercontent.com/eugeniogarcia/pachyderm/master/data/personas/Personas3.txt
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
pachctl create pipeline -f https://raw.githubusercontent.com/eugeniogarcia/pachyderm/master/pipelines/edades-agrega.json
pachctl create pipeline -f https://raw.githubusercontent.com/eugeniogarcia/pachyderm/master/pipelines/profesion.json
pachctl create pipeline -f https://raw.githubusercontent.com/eugeniogarcia/pachyderm/master/pipelines/profesion-agrega.json
```

```sh
pachctl list repo

NAME             CREATED            SIZE (MASTER) ACCESS LEVEL
profesion-agrega About a minute ago 180B          OWNER        Output repo for pipeline profesion-agrega.
profesion        About a minute ago 180B          OWNER        Output repo for pipeline profesion.
edades-agrega    2 minutes ago      277B          OWNER        Output repo for pipeline edades-agrega.
edades           2 minutes ago      277B          OWNER        Output repo for pipeline edades.
personas         3 minutes ago      260B          OWNER
```

```sh
pachctl list file edades@master

NAME               TYPE SIZE
/EdadPersonas1.txt file 101B
/EdadPersonas2.txt file 86B
/EdadPersonas3.txt file 90B
```

```sh
pachctl list file edades-agrega@master

NAME                                TYPE SIZE
/EdadPersonas637157472648055685.txt file 277B
```

```sh
pachctl get file edades-agrega@master:EdadPersonas637157472648055685.txt

3;Eugenio;Garcia Zach;V;2004;H;Madrid;15
4;Clara;Garcia Zach;H;2006;H;Torrelodones;13
1;Eugenio;Garcia San Martin;V;1969;P;San Pedro Bercianos;50
2;Vera Carmen;Zach;H;1973;M;Salzburgo;46
5;Leah;Garcia Zach;H;2008;H;Torrelodones;11
6;Nicolas;Garcia Zach;V;2011;H;Torrelodones;8
```

efecto /* y /

orden segun dependencias
```sh
pachctl delete pipeline profesion-agrega
pachctl delete pipeline edades-agrega
pachctl delete pipeline profesion
pachctl delete pipeline edades
pachctl delete repo personas
```

overwrite

```sh
pachctl put file personas@master:Personas1.txt -f https://raw.githubusercontent.com/eugeniogarcia/pachyderm/master/data/personas/Personas1.txt

pachctl get file personas@master:Personas1.txt

1;Eugenio;Garcia San Martin;V;1969;P;San Pedro Bercianos
2;Vera Carmen;Zach;H;1973;M;Salzburgo
1;Eugenio;Garcia San Martin;V;1969;P;San Pedro Bercianos
2;Vera Carmen;Zach;H;1973;M;Salzburgo
```

```sh
pachctl put file personas@master:Personas2.txt -o -f https://raw.githubusercontent.com/eugeniogarcia/pachyderm/master/data/personas/Personas2.txt

pachctl get file personas@master:Personas2.txt

3;Eugenio;Garcia Zach;V;2004;H;Madrid
4;Clara;Garcia Zach;H;2006;H;Torrelodones
```
 
significado de +, DL y UL

```sh
pachctl list repo

ID                               PIPELINE         STARTED       DURATION  RESTART PROGRESS  DL   UL   STATE
df610e990cc940568a5ae4bc4f44bea1 profesion        2 minutes ago 1 second  0       0 + 3 / 3 0B   0B   success
ecc913b1d9914d03a78e64fdb3777238 profesion-agrega 2 minutes ago 1 second  0       0 + 1 / 1 0B   0B   success
48a18e495ba94feeaeb7687c0797cbfa edades-agrega    2 minutes ago 1 second  0       0 + 1 / 1 0B   0B   success
b86bfa791edf443891e7fad28d7ac676 edades           2 minutes ago 1 second  0       0 + 3 / 3 0B   0B   success
60ad7bc9892d4cb5b4ecd288ee6b0e94 profesion        2 minutes ago 3 seconds 0       1 + 2 / 3 202B 138B success
fe5e0275b5ca42be8032c3e96b62c05c profesion-agrega 2 minutes ago 3 seconds 0       1 + 0 / 1 378B 249B success
193164fc4f484a01a4addd765d350149 edades-agrega    2 minutes ago 3 seconds 0       1 + 0 / 1 355B 378B success
3f2442c12b994482a7b60f790b17317f edades           2 minutes ago 3 seconds 0       1 + 2 / 3 190B 202B success
0aee8308ee8742c1a410d091baae06c7 profesion-agrega 8 minutes ago 2 seconds 0       1 + 0 / 1 277B 180B success
087c55214016422b92c960ecec22beb7 profesion        8 minutes ago 7 seconds 0       3 + 0 / 3 277B 180B success
f8e439820df14c54bc0aad8094f50bd0 edades-agrega    8 minutes ago 3 seconds 0       1 + 0 / 1 260B 277B success
2a491e3d52ec4783bf91229b0a4d8eaf edades           8 minutes ago 5 seconds 0       3 + 0 / 3 260B 277B success
```
