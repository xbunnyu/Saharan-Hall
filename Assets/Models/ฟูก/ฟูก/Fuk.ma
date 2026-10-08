//Maya ASCII 2025ff03 scene
//Name: Fuk.ma
//Last modified: Sat, Sep 19, 2026 03:40:07 AM
//Codeset: 874
requires maya "2025ff03";
requires -nodeType "aiOptions" -nodeType "aiAOVDriver" -nodeType "aiAOVFilter" -nodeType "aiImagerDenoiserOidn"
		 "mtoa" "5.4.5";
currentUnit -l centimeter -a degree -t film;
fileInfo "application" "maya";
fileInfo "product" "Maya 2025";
fileInfo "version" "2025";
fileInfo "cutIdentifier" "202409190603-cbdc5a7e54";
fileInfo "osv" "Windows 11 Pro v2009 (Build: 26200)";
fileInfo "UUID" "DF05D0BC-4260-3758-F250-D5B3B34A0234";
createNode transform -s -n "persp";
	rename -uid "0CF20CDD-4FE7-68C1-B583-F2B36D8A84E5";
	setAttr ".v" no;
	setAttr ".t" -type "double3" -13.981633455131766 14.702925750215314 27.035551859990676 ;
	setAttr ".r" -type "double3" -25.538352729361083 -388.99999999995077 9.0912503328577779e-16 ;
createNode camera -s -n "perspShape" -p "persp";
	rename -uid "0788896E-430F-37FA-2530-21A2A870CED5";
	setAttr -k off ".v" no;
	setAttr ".fl" 34.999999999999993;
	setAttr ".coi" 33.247525255213226;
	setAttr ".imn" -type "string" "persp";
	setAttr ".den" -type "string" "persp_depth";
	setAttr ".man" -type "string" "persp_mask";
	setAttr ".hc" -type "string" "viewSet -p %camera";
createNode transform -s -n "top";
	rename -uid "BBF5BB9F-4BCF-0522-9409-3B9C75187E7A";
	setAttr ".v" no;
	setAttr ".t" -type "double3" 0 1000.1 0 ;
	setAttr ".r" -type "double3" -90 0 0 ;
createNode camera -s -n "topShape" -p "top";
	rename -uid "9593EFA3-4DED-EF29-9C6F-B5B171F27B6B";
	setAttr -k off ".v" no;
	setAttr ".rnd" no;
	setAttr ".coi" 1000.1;
	setAttr ".ow" 30;
	setAttr ".imn" -type "string" "top";
	setAttr ".den" -type "string" "top_depth";
	setAttr ".man" -type "string" "top_mask";
	setAttr ".hc" -type "string" "viewSet -t %camera";
	setAttr ".o" yes;
createNode transform -s -n "front";
	rename -uid "75985A15-4F03-718F-9EA2-B8AC6C61002F";
	setAttr ".v" no;
	setAttr ".t" -type "double3" 0 0 1000.1 ;
createNode camera -s -n "frontShape" -p "front";
	rename -uid "95B3346C-4ACA-67EC-7AB7-7BA9116CED02";
	setAttr -k off ".v" no;
	setAttr ".rnd" no;
	setAttr ".coi" 1000.1;
	setAttr ".ow" 30;
	setAttr ".imn" -type "string" "front";
	setAttr ".den" -type "string" "front_depth";
	setAttr ".man" -type "string" "front_mask";
	setAttr ".hc" -type "string" "viewSet -f %camera";
	setAttr ".o" yes;
createNode transform -s -n "side";
	rename -uid "742ED655-431B-5086-D993-D18E9D8A110E";
	setAttr ".v" no;
	setAttr ".t" -type "double3" 1000.1 0.29534558430929525 0.24671560937800208 ;
	setAttr ".r" -type "double3" 0 90 0 ;
createNode camera -s -n "sideShape" -p "side";
	rename -uid "EF87B044-4377-5B58-FC4D-F5BB2D98C5CB";
	setAttr -k off ".v" no;
	setAttr ".rnd" no;
	setAttr ".coi" 1000.1;
	setAttr ".ow" 10.793631653449999;
	setAttr ".imn" -type "string" "side";
	setAttr ".den" -type "string" "side_depth";
	setAttr ".man" -type "string" "side_mask";
	setAttr ".hc" -type "string" "viewSet -s %camera";
	setAttr ".o" yes;
createNode transform -n "pCube1";
	rename -uid "910B56AD-425B-5A28-42AA-1E8884B68FEF";
	setAttr ".t" -type "double3" 0 0.39591102337861583 0 ;
	setAttr ".s" -type "double3" 3.6904568658849213 0.57372768141992736 6.0603784982722004 ;
createNode mesh -n "pCubeShape1" -p "pCube1";
	rename -uid "1D761F44-44E7-08B8-6B84-AF8B98C4576D";
	setAttr -k off ".v";
	setAttr ".vir" yes;
	setAttr ".vif" yes;
	setAttr ".pv" -type "double2" 0.21700742972804177 0.50539789476425856 ;
	setAttr ".uvst[0].uvsn" -type "string" "map1";
	setAttr ".cuvs" -type "string" "map1";
	setAttr ".dcc" -type "string" "Ambient+Diffuse";
	setAttr ".covm[0]"  0 1 1;
	setAttr ".cdvm[0]"  0 1 1;
	setAttr ".dr" 3;
	setAttr ".dsm" 2;
createNode transform -n "pCube2";
	rename -uid "EFBDE1AB-409A-E7C2-5BB4-448161FC5EE6";
	setAttr ".t" -type "double3" 0.53128309571122112 0.71036167758344526 1.6763871674724689 ;
	setAttr ".s" -type "double3" 1.4002473749445541 0.056893799540346138 1.4002473749445541 ;
createNode mesh -n "pCubeShape2" -p "pCube2";
	rename -uid "988F55FC-4E47-4761-D937-709539699474";
	setAttr -k off ".v";
	setAttr ".vir" yes;
	setAttr ".vif" yes;
	setAttr ".pv" -type "double2" 0.84826336589747364 0.48205770408016391 ;
	setAttr ".uvst[0].uvsn" -type "string" "map1";
	setAttr ".cuvs" -type "string" "map1";
	setAttr ".dcc" -type "string" "Ambient+Diffuse";
	setAttr ".covm[0]"  0 1 1;
	setAttr ".cdvm[0]"  0 1 1;
	setAttr ".dr" 3;
	setAttr ".dsm" 2;
createNode transform -n "pCube3";
	rename -uid "1EECF8ED-4226-FB04-B1FD-7CB357A72A2F";
	setAttr ".t" -type "double3" 0.53128309571122112 0.76204081500579179 1.6763871674724689 ;
	setAttr ".s" -type "double3" 1.4002473749445541 0.056893799540346138 1.4002473749445541 ;
createNode mesh -n "pCubeShape3" -p "pCube3";
	rename -uid "AB67E26E-4762-FA83-6246-7D9A79B5F2BB";
	setAttr -k off ".v";
	setAttr ".vir" yes;
	setAttr ".vif" yes;
	setAttr ".pv" -type "double2" 0.5646372267335027 0.49287593843890187 ;
	setAttr ".uvst[0].uvsn" -type "string" "map1";
	setAttr ".cuvs" -type "string" "map1";
	setAttr ".dcc" -type "string" "Ambient+Diffuse";
	setAttr ".covm[0]"  0 1 1;
	setAttr ".cdvm[0]"  0 1 1;
	setAttr ".dr" 3;
	setAttr ".dsm" 2;
createNode mesh -n "polySurfaceShape1" -p "pCube3";
	rename -uid "1EFDC15D-40E3-A4A4-5398-B7BA798E9DE9";
	setAttr -k off ".v";
	setAttr ".io" yes;
	setAttr ".vir" yes;
	setAttr ".vif" yes;
	setAttr -s 6 ".gtag";
	setAttr ".gtag[0].gtagnm" -type "string" "back";
	setAttr ".gtag[0].gtagcmp" -type "componentList" 4 "f[2]" "f[8]" "f[12]" "f[33:35]";
	setAttr ".gtag[1].gtagnm" -type "string" "bottom";
	setAttr ".gtag[1].gtagcmp" -type "componentList" 5 "f[3]" "f[9]" "f[13]" "f[15:17]" "f[23:25]";
	setAttr ".gtag[2].gtagnm" -type "string" "front";
	setAttr ".gtag[2].gtagcmp" -type "componentList" 4 "f[0]" "f[6]" "f[10]" "f[39:41]";
	setAttr ".gtag[3].gtagnm" -type "string" "left";
	setAttr ".gtag[3].gtagcmp" -type "componentList" 4 "f[5]" "f[14]" "f[22]" "f[30:32]";
	setAttr ".gtag[4].gtagnm" -type "string" "right";
	setAttr ".gtag[4].gtagcmp" -type "componentList" 4 "f[4]" "f[18]" "f[26]" "f[36:38]";
	setAttr ".gtag[5].gtagnm" -type "string" "top";
	setAttr ".gtag[5].gtagcmp" -type "componentList" 5 "f[1]" "f[7]" "f[11]" "f[19:21]" "f[27:29]";
	setAttr ".pv" -type "double2" 0.5646372267335027 0.49287593843890187 ;
	setAttr ".uvst[0].uvsn" -type "string" "map1";
	setAttr -s 54 ".uvst[0].uvsp[0:53]" -type "float2" 0.84307969 0.74295217
		 0.83517343 0.72220558 0.83993095 0.7221272 0.84356105 0.74488658 0.84618503 0.74836904
		 0.84515953 0.722 0.81132102 0.77063942 0.81123799 0.75162351 0.59938735 0.74264485
		 0.59447718 0.72481561 0.59895426 0.72461009 0.60124665 0.74045849 0.81078941 0.74154145
		 0.81229758 0.72237307 0.8264553 0.50820452 0.80863607 0.50635129 0.80868447 0.50185513
		 0.82844174 0.50612056 0.81106275 0.7463209 0.83386213 0.52583134 0.8293432 0.52611071
		 0.80875123 0.49683166 0.81226724 0.96453804 0.83891255 0.52550542 0.83142835 0.50298232
		 0.59621733 0.53070921 0.59173357 0.53063613 0.59620738 0.51080745 0.59821284 0.51286215
		 0.80878556 0.52700901 0.62090713 0.74275541 0.62079185 0.74723899 0.62111753 0.72405928
		 0.61841708 0.53064257 0.61772066 0.51001805 0.61749548 0.50553101 0.59901488 0.96554381
		 0.6216765 0.96543002 0.61723244 0.50051773 0.59914422 0.9865883 0.5980615 0.77144563
		 0.62073481 0.77133286 0.62065405 0.75224692 0.59654635 0.74589592 0.81237 0.98555565
		 0.62178075 0.98645669 0.59320021 0.50775874 0.58946919 0.72505742 0.58672094 0.53054059
		 0.83211333 0.77053791 0.83306909 0.9644379 0.83319533 0.98544961 0.59796333 0.75238323
		 0.83201545 0.75151455;
	setAttr ".cuvs" -type "string" "map1";
	setAttr ".dcc" -type "string" "Ambient+Diffuse";
	setAttr ".covm[0]"  0 1 1;
	setAttr ".cdvm[0]"  0 1 1;
	setAttr -s 44 ".vt[0:43]"  -0.5 -0.5 0.50000012 0.49999994 -0.5 0.50000012
		 -0.5 0.50000095 0.50000012 0.49999994 0.50000095 0.50000012 -0.5 0.50000095 -0.49999988
		 0.49999994 0.50000095 -0.49999988 -0.5 -0.5 -0.49999988 0.49999994 -0.5 -0.49999988
		 0.41017598 -0.5 0.50000012 0.41017598 0.50000095 0.50000012 0.41017598 0.50000095 -0.49999988
		 0.41017598 -0.5 -0.49999988 -0.41861698 -0.5 0.50000012 -0.41861698 0.50000095 0.50000012
		 -0.41861698 0.50000095 -0.49999988 -0.41861698 -0.5 -0.49999988 -0.5 0.50000095 -0.40318888
		 -0.5 -0.5 -0.40318859 -0.41861698 -0.5 -0.40318859 0.41017598 -0.5 -0.40318859 0.49999994 -0.5 -0.40318859
		 0.49999994 0.50000095 -0.40318888 0.41017598 0.50000095 -0.40318888 -0.41861698 0.50000095 -0.40318888
		 -0.5 0.50000095 0.41111648 -0.5 -0.5 0.41111636 -0.41861698 -0.5 0.41111636 0.41017598 -0.5 0.41111636
		 0.49999994 -0.5 0.41111636 0.49999994 0.50000095 0.41111648 0.41017598 0.50000095 0.41111648
		 -0.41861698 0.50000095 0.41111648 -0.5 -0.025267601 0.50000012 -0.5 -0.025267601 0.41111648
		 -0.5 -0.025267601 -0.40318877 -0.5 -0.025267601 -0.49999988 -0.41861698 -0.025267601 -0.49999988
		 0.41017598 -0.025267601 -0.49999988 0.49999994 -0.025267601 -0.49999988 0.49999994 -0.025267601 -0.40318877
		 0.49999994 -0.025267601 0.41111648 0.49999994 -0.025267601 0.50000012 0.41017598 -0.025267601 0.50000012
		 -0.41861695 -0.025267601 0.50000012;
	setAttr -s 84 ".ed[0:83]"  0 12 0 2 13 0 4 14 0 6 15 0 0 32 0 1 41 0
		 2 24 0 3 29 0 4 35 0 5 38 0 6 17 0 7 20 0 8 1 0 9 3 0 10 5 0 11 7 0 8 42 1 9 30 1
		 10 37 1 11 19 1 12 8 0 13 9 0 14 10 0 15 11 0 12 43 1 13 31 1 14 36 1 15 18 1 16 4 0
		 17 25 0 18 26 1 19 27 1 20 28 0 21 5 0 22 10 1 23 14 1 16 34 1 17 18 1 18 19 1 19 20 1
		 20 39 1 21 22 1 22 23 1 23 16 1 24 16 0 25 0 0 26 12 1 27 8 1 28 1 0 29 21 0 30 22 1
		 31 23 1 24 33 1 25 26 1 26 27 1 27 28 1 28 40 1 29 30 1 30 31 1 31 24 1 32 2 0 33 25 1
		 34 17 1 35 6 0 36 15 1 37 11 1 38 7 0 39 21 1 40 29 1 41 3 0 42 9 1 43 13 1 32 33 1
		 33 34 1 34 35 1 35 36 1 36 37 1 37 38 1 38 39 1 39 40 1 40 41 1 41 42 1 42 43 1 43 32 1;
	setAttr -s 42 -ch 168 ".fc[0:41]" -type "polyFaces" 
		f 4 0 24 83 -5
		mu 0 4 0 1 2 3
		f 4 1 25 59 -7
		mu 0 4 53 49 6 7
		f 4 75 64 -4 -64
		mu 0 4 8 9 10 11
		f 4 53 46 -1 -46
		mu 0 4 12 13 1 0
		f 4 -49 56 80 -6
		mu 0 4 14 15 16 17
		f 4 72 61 45 4
		mu 0 4 3 18 12 0
		f 4 81 -17 12 5
		mu 0 4 17 19 20 14
		f 4 57 -18 13 7
		mu 0 4 44 22 50 51
		f 4 -66 77 66 -16
		mu 0 4 25 26 27 28
		f 4 -48 55 48 -13
		mu 0 4 20 29 15 14
		f 4 82 -25 20 16
		mu 0 4 19 2 1 20
		f 4 58 -26 21 17
		mu 0 4 22 6 49 50
		f 4 -65 76 65 -24
		mu 0 4 10 9 26 25
		f 4 -47 54 47 -21
		mu 0 4 1 13 29 20
		f 4 10 -63 74 63
		mu 0 4 11 30 31 8
		f 4 3 27 -38 -11
		mu 0 4 11 10 32 30
		f 4 -39 -28 23 19
		mu 0 4 33 32 10 25
		f 4 -40 -20 15 11
		mu 0 4 34 33 25 28
		f 4 78 -41 -12 -67
		mu 0 4 27 35 34 28
		f 4 -35 -42 33 -15
		mu 0 4 36 37 45 39
		f 4 -36 -43 34 -23
		mu 0 4 40 41 37 36
		f 4 -44 35 -3 -29
		mu 0 4 42 41 40 52
		f 4 73 62 29 -62
		mu 0 4 18 31 30 12
		f 4 37 30 -54 -30
		mu 0 4 30 32 13 12
		f 4 -55 -31 38 31
		mu 0 4 29 13 32 33
		f 4 -56 -32 39 32
		mu 0 4 15 29 33 34
		f 4 79 -57 -33 40
		mu 0 4 35 16 15 34
		f 4 41 -51 -58 49
		mu 0 4 45 37 22 44
		f 4 42 -52 -59 50
		mu 0 4 37 41 6 22
		f 4 -60 51 43 -45
		mu 0 4 7 6 41 42
		f 4 52 -73 60 6
		mu 0 4 7 18 3 4
		f 4 36 -74 -53 44
		mu 0 4 42 31 18 7
		f 4 -75 -37 28 8
		mu 0 4 8 31 42 43
		f 4 2 26 -76 -9
		mu 0 4 43 47 9 8
		f 4 -77 -27 22 18
		mu 0 4 26 9 47 48
		f 4 -78 -19 14 9
		mu 0 4 27 26 48 46
		f 4 -68 -79 -10 -34
		mu 0 4 38 35 27 46
		f 4 -69 -80 67 -50
		mu 0 4 21 16 35 38
		f 4 -81 68 -8 -70
		mu 0 4 17 16 21 24
		f 4 -71 -82 69 -14
		mu 0 4 23 19 17 24
		f 4 -72 -83 70 -22
		mu 0 4 5 2 19 23
		f 4 -84 71 -2 -61
		mu 0 4 3 2 5 4;
	setAttr ".cd" -type "dataPolyComponent" Index_Data Edge 0 ;
	setAttr ".cvd" -type "dataPolyComponent" Index_Data Vertex 0 ;
	setAttr ".pd[0]" -type "dataPolyComponent" Index_Data UV 2 
		7 0 
		42 0 ;
	setAttr ".hfd" -type "dataPolyComponent" Index_Data Face 0 ;
	setAttr ".dr" 3;
	setAttr ".dsm" 2;
createNode transform -n "pCube4";
	rename -uid "2AF9338E-4860-90A8-3367-C19ED209F402";
	setAttr ".t" -type "double3" 0 0.06385791705916799 0.33244645588015886 ;
	setAttr ".s" -type "double3" 6.4376668506678785 0.10566578697485401 8.5383516679350535 ;
createNode mesh -n "pCubeShape4" -p "pCube4";
	rename -uid "1F2A77DD-4F0E-33B1-3EBE-57B877070390";
	setAttr -k off ".v";
	setAttr ".vir" yes;
	setAttr ".vif" yes;
	setAttr ".pv" -type "double2" 0.19652802497148514 0.50000008195638657 ;
	setAttr ".uvst[0].uvsn" -type "string" "map1";
	setAttr ".cuvs" -type "string" "map1";
	setAttr ".dcc" -type "string" "Ambient+Diffuse";
	setAttr ".covm[0]"  0 1 1;
	setAttr ".cdvm[0]"  0 1 1;
createNode lightLinker -s -n "lightLinker1";
	rename -uid "76EB47B5-4D54-23C8-17C9-B7B940A08B05";
	setAttr -s 2 ".lnk";
	setAttr -s 2 ".slnk";
createNode shapeEditorManager -n "shapeEditorManager";
	rename -uid "1112989D-4D0F-6A31-B508-528703E72A3C";
createNode poseInterpolatorManager -n "poseInterpolatorManager";
	rename -uid "1A2109D3-445B-EC61-F21C-87B0173F60A5";
createNode displayLayerManager -n "layerManager";
	rename -uid "C5ED35C4-44AC-B475-2666-3DB746A45DDB";
createNode displayLayer -n "defaultLayer";
	rename -uid "2D5B290D-4B60-F084-F8C2-C8877E49717E";
	setAttr ".ufem" -type "stringArray" 0  ;
createNode renderLayerManager -n "renderLayerManager";
	rename -uid "35445930-4ED2-3EFE-E405-3CAE90609AA2";
createNode renderLayer -n "defaultRenderLayer";
	rename -uid "B31CA48C-4D7A-99D0-E1A1-95923FAC76FE";
	setAttr ".g" yes;
createNode polyCube -n "polyCube1";
	rename -uid "BB0849EC-4697-21B5-C71E-7893E9A4FB16";
	setAttr ".cuv" 4;
createNode script -n "uiConfigurationScriptNode";
	rename -uid "B8DBE15A-4986-1D40-6254-C3A0626BD380";
	setAttr ".b" -type "string" (
		"// Maya Mel UI Configuration File.\n//\n//  This script is machine generated.  Edit at your own risk.\n//\n//\n\nglobal string $gMainPane;\nif (`paneLayout -exists $gMainPane`) {\n\n\tglobal int $gUseScenePanelConfig;\n\tint    $useSceneConfig = $gUseScenePanelConfig;\n\tint    $nodeEditorPanelVisible = stringArrayContains(\"nodeEditorPanel1\", `getPanel -vis`);\n\tint    $nodeEditorWorkspaceControlOpen = (`workspaceControl -exists nodeEditorPanel1Window` && `workspaceControl -q -visible nodeEditorPanel1Window`);\n\tint    $menusOkayInPanels = `optionVar -q allowMenusInPanels`;\n\tint    $nVisPanes = `paneLayout -q -nvp $gMainPane`;\n\tint    $nPanes = 0;\n\tstring $editorName;\n\tstring $panelName;\n\tstring $itemFilterName;\n\tstring $panelConfig;\n\n\t//\n\t//  get current state of the UI\n\t//\n\tsceneUIReplacement -update $gMainPane;\n\n\t$panelName = `sceneUIReplacement -getNextPanel \"modelPanel\" (localizedPanelLabel(\"Top View\")) `;\n\tif (\"\" != $panelName) {\n\t\t$label = `panel -q -label $panelName`;\n\t\tmodelPanel -edit -l (localizedPanelLabel(\"Top View\")) -mbv $menusOkayInPanels  $panelName;\n"
		+ "\t\t$editorName = $panelName;\n        modelEditor -e \n            -camera \"|top\" \n            -useInteractiveMode 0\n            -displayLights \"default\" \n            -displayAppearance \"smoothShaded\" \n            -activeOnly 0\n            -ignorePanZoom 0\n            -wireframeOnShaded 0\n            -headsUpDisplay 1\n            -holdOuts 1\n            -selectionHiliteDisplay 1\n            -useDefaultMaterial 0\n            -bufferMode \"double\" \n            -twoSidedLighting 0\n            -backfaceCulling 0\n            -xray 0\n            -jointXray 0\n            -activeComponentsXray 0\n            -displayTextures 0\n            -smoothWireframe 0\n            -lineWidth 1\n            -textureAnisotropic 0\n            -textureHilight 1\n            -textureSampling 2\n            -textureDisplay \"modulate\" \n            -textureMaxSize 32768\n            -fogging 0\n            -fogSource \"fragment\" \n            -fogMode \"linear\" \n            -fogStart 0\n            -fogEnd 100\n            -fogDensity 0.1\n            -fogColor 0.5 0.5 0.5 1 \n"
		+ "            -depthOfFieldPreview 1\n            -maxConstantTransparency 1\n            -rendererName \"vp2Renderer\" \n            -objectFilterShowInHUD 1\n            -isFiltered 0\n            -colorResolution 256 256 \n            -bumpResolution 512 512 \n            -textureCompression 0\n            -transparencyAlgorithm \"frontAndBackCull\" \n            -transpInShadows 0\n            -cullingOverride \"none\" \n            -lowQualityLighting 0\n            -maximumNumHardwareLights 1\n            -occlusionCulling 0\n            -shadingModel 0\n            -useBaseRenderer 0\n            -useReducedRenderer 0\n            -smallObjectCulling 0\n            -smallObjectThreshold -1 \n            -interactiveDisableShadows 0\n            -interactiveBackFaceCull 0\n            -sortTransparent 1\n            -controllers 1\n            -nurbsCurves 1\n            -nurbsSurfaces 1\n            -polymeshes 1\n            -subdivSurfaces 1\n            -planes 1\n            -lights 1\n            -cameras 1\n            -controlVertices 1\n"
		+ "            -hulls 1\n            -grid 1\n            -imagePlane 1\n            -joints 1\n            -ikHandles 1\n            -deformers 1\n            -dynamics 1\n            -particleInstancers 1\n            -fluids 1\n            -hairSystems 1\n            -follicles 1\n            -nCloths 1\n            -nParticles 1\n            -nRigids 1\n            -dynamicConstraints 1\n            -locators 1\n            -manipulators 1\n            -pluginShapes 1\n            -dimensions 1\n            -handles 1\n            -pivots 1\n            -textures 1\n            -strokes 1\n            -motionTrails 1\n            -clipGhosts 1\n            -bluePencil 1\n            -greasePencils 0\n            -excludeObjectPreset \"All\" \n            -shadows 0\n            -captureSequenceNumber -1\n            -width 1\n            -height 1\n            -sceneRenderFilter 0\n            $editorName;\n        modelEditor -e -viewSelected 0 $editorName;\n\t\tif (!$useSceneConfig) {\n\t\t\tpanel -e -l $label $panelName;\n\t\t}\n\t}\n\n\n\t$panelName = `sceneUIReplacement -getNextPanel \"modelPanel\" (localizedPanelLabel(\"Side View\")) `;\n"
		+ "\tif (\"\" != $panelName) {\n\t\t$label = `panel -q -label $panelName`;\n\t\tmodelPanel -edit -l (localizedPanelLabel(\"Side View\")) -mbv $menusOkayInPanels  $panelName;\n\t\t$editorName = $panelName;\n        modelEditor -e \n            -camera \"|side\" \n            -useInteractiveMode 0\n            -displayLights \"default\" \n            -displayAppearance \"smoothShaded\" \n            -activeOnly 0\n            -ignorePanZoom 0\n            -wireframeOnShaded 0\n            -headsUpDisplay 1\n            -holdOuts 1\n            -selectionHiliteDisplay 1\n            -useDefaultMaterial 0\n            -bufferMode \"double\" \n            -twoSidedLighting 0\n            -backfaceCulling 0\n            -xray 0\n            -jointXray 0\n            -activeComponentsXray 0\n            -displayTextures 0\n            -smoothWireframe 0\n            -lineWidth 1\n            -textureAnisotropic 0\n            -textureHilight 1\n            -textureSampling 2\n            -textureDisplay \"modulate\" \n            -textureMaxSize 32768\n            -fogging 0\n"
		+ "            -fogSource \"fragment\" \n            -fogMode \"linear\" \n            -fogStart 0\n            -fogEnd 100\n            -fogDensity 0.1\n            -fogColor 0.5 0.5 0.5 1 \n            -depthOfFieldPreview 1\n            -maxConstantTransparency 1\n            -rendererName \"vp2Renderer\" \n            -objectFilterShowInHUD 1\n            -isFiltered 0\n            -colorResolution 256 256 \n            -bumpResolution 512 512 \n            -textureCompression 0\n            -transparencyAlgorithm \"frontAndBackCull\" \n            -transpInShadows 0\n            -cullingOverride \"none\" \n            -lowQualityLighting 0\n            -maximumNumHardwareLights 1\n            -occlusionCulling 0\n            -shadingModel 0\n            -useBaseRenderer 0\n            -useReducedRenderer 0\n            -smallObjectCulling 0\n            -smallObjectThreshold -1 \n            -interactiveDisableShadows 0\n            -interactiveBackFaceCull 0\n            -sortTransparent 1\n            -controllers 1\n            -nurbsCurves 1\n"
		+ "            -nurbsSurfaces 1\n            -polymeshes 1\n            -subdivSurfaces 1\n            -planes 1\n            -lights 1\n            -cameras 1\n            -controlVertices 1\n            -hulls 1\n            -grid 1\n            -imagePlane 1\n            -joints 1\n            -ikHandles 1\n            -deformers 1\n            -dynamics 1\n            -particleInstancers 1\n            -fluids 1\n            -hairSystems 1\n            -follicles 1\n            -nCloths 1\n            -nParticles 1\n            -nRigids 1\n            -dynamicConstraints 1\n            -locators 1\n            -manipulators 1\n            -pluginShapes 1\n            -dimensions 1\n            -handles 1\n            -pivots 1\n            -textures 1\n            -strokes 1\n            -motionTrails 1\n            -clipGhosts 1\n            -bluePencil 1\n            -greasePencils 0\n            -excludeObjectPreset \"All\" \n            -shadows 0\n            -captureSequenceNumber -1\n            -width 1\n            -height 1\n            -sceneRenderFilter 0\n"
		+ "            $editorName;\n        modelEditor -e -viewSelected 0 $editorName;\n\t\tif (!$useSceneConfig) {\n\t\t\tpanel -e -l $label $panelName;\n\t\t}\n\t}\n\n\n\t$panelName = `sceneUIReplacement -getNextPanel \"modelPanel\" (localizedPanelLabel(\"Front View\")) `;\n\tif (\"\" != $panelName) {\n\t\t$label = `panel -q -label $panelName`;\n\t\tmodelPanel -edit -l (localizedPanelLabel(\"Front View\")) -mbv $menusOkayInPanels  $panelName;\n\t\t$editorName = $panelName;\n        modelEditor -e \n            -camera \"|front\" \n            -useInteractiveMode 0\n            -displayLights \"default\" \n            -displayAppearance \"smoothShaded\" \n            -activeOnly 0\n            -ignorePanZoom 0\n            -wireframeOnShaded 0\n            -headsUpDisplay 1\n            -holdOuts 1\n            -selectionHiliteDisplay 1\n            -useDefaultMaterial 0\n            -bufferMode \"double\" \n            -twoSidedLighting 0\n            -backfaceCulling 0\n            -xray 0\n            -jointXray 0\n            -activeComponentsXray 0\n            -displayTextures 0\n"
		+ "            -smoothWireframe 0\n            -lineWidth 1\n            -textureAnisotropic 0\n            -textureHilight 1\n            -textureSampling 2\n            -textureDisplay \"modulate\" \n            -textureMaxSize 32768\n            -fogging 0\n            -fogSource \"fragment\" \n            -fogMode \"linear\" \n            -fogStart 0\n            -fogEnd 100\n            -fogDensity 0.1\n            -fogColor 0.5 0.5 0.5 1 \n            -depthOfFieldPreview 1\n            -maxConstantTransparency 1\n            -rendererName \"vp2Renderer\" \n            -objectFilterShowInHUD 1\n            -isFiltered 0\n            -colorResolution 256 256 \n            -bumpResolution 512 512 \n            -textureCompression 0\n            -transparencyAlgorithm \"frontAndBackCull\" \n            -transpInShadows 0\n            -cullingOverride \"none\" \n            -lowQualityLighting 0\n            -maximumNumHardwareLights 1\n            -occlusionCulling 0\n            -shadingModel 0\n            -useBaseRenderer 0\n            -useReducedRenderer 0\n"
		+ "            -smallObjectCulling 0\n            -smallObjectThreshold -1 \n            -interactiveDisableShadows 0\n            -interactiveBackFaceCull 0\n            -sortTransparent 1\n            -controllers 1\n            -nurbsCurves 1\n            -nurbsSurfaces 1\n            -polymeshes 1\n            -subdivSurfaces 1\n            -planes 1\n            -lights 1\n            -cameras 1\n            -controlVertices 1\n            -hulls 1\n            -grid 1\n            -imagePlane 1\n            -joints 1\n            -ikHandles 1\n            -deformers 1\n            -dynamics 1\n            -particleInstancers 1\n            -fluids 1\n            -hairSystems 1\n            -follicles 1\n            -nCloths 1\n            -nParticles 1\n            -nRigids 1\n            -dynamicConstraints 1\n            -locators 1\n            -manipulators 1\n            -pluginShapes 1\n            -dimensions 1\n            -handles 1\n            -pivots 1\n            -textures 1\n            -strokes 1\n            -motionTrails 1\n            -clipGhosts 1\n"
		+ "            -bluePencil 1\n            -greasePencils 0\n            -excludeObjectPreset \"All\" \n            -shadows 0\n            -captureSequenceNumber -1\n            -width 1\n            -height 1\n            -sceneRenderFilter 0\n            $editorName;\n        modelEditor -e -viewSelected 0 $editorName;\n\t\tif (!$useSceneConfig) {\n\t\t\tpanel -e -l $label $panelName;\n\t\t}\n\t}\n\n\n\t$panelName = `sceneUIReplacement -getNextPanel \"modelPanel\" (localizedPanelLabel(\"Persp View\")) `;\n\tif (\"\" != $panelName) {\n\t\t$label = `panel -q -label $panelName`;\n\t\tmodelPanel -edit -l (localizedPanelLabel(\"Persp View\")) -mbv $menusOkayInPanels  $panelName;\n\t\t$editorName = $panelName;\n        modelEditor -e \n            -camera \"|persp\" \n            -useInteractiveMode 0\n            -displayLights \"default\" \n            -displayAppearance \"smoothShaded\" \n            -activeOnly 0\n            -ignorePanZoom 0\n            -wireframeOnShaded 1\n            -headsUpDisplay 1\n            -holdOuts 1\n            -selectionHiliteDisplay 1\n            -useDefaultMaterial 0\n"
		+ "            -bufferMode \"double\" \n            -twoSidedLighting 0\n            -backfaceCulling 0\n            -xray 0\n            -jointXray 0\n            -activeComponentsXray 0\n            -displayTextures 0\n            -smoothWireframe 0\n            -lineWidth 1\n            -textureAnisotropic 0\n            -textureHilight 1\n            -textureSampling 2\n            -textureDisplay \"modulate\" \n            -textureMaxSize 32768\n            -fogging 0\n            -fogSource \"fragment\" \n            -fogMode \"linear\" \n            -fogStart 0\n            -fogEnd 100\n            -fogDensity 0.1\n            -fogColor 0.5 0.5 0.5 1 \n            -depthOfFieldPreview 1\n            -maxConstantTransparency 1\n            -rendererName \"vp2Renderer\" \n            -objectFilterShowInHUD 1\n            -isFiltered 0\n            -colorResolution 256 256 \n            -bumpResolution 512 512 \n            -textureCompression 0\n            -transparencyAlgorithm \"frontAndBackCull\" \n            -transpInShadows 0\n            -cullingOverride \"none\" \n"
		+ "            -lowQualityLighting 0\n            -maximumNumHardwareLights 1\n            -occlusionCulling 0\n            -shadingModel 0\n            -useBaseRenderer 0\n            -useReducedRenderer 0\n            -smallObjectCulling 0\n            -smallObjectThreshold -1 \n            -interactiveDisableShadows 0\n            -interactiveBackFaceCull 0\n            -sortTransparent 1\n            -controllers 1\n            -nurbsCurves 1\n            -nurbsSurfaces 1\n            -polymeshes 1\n            -subdivSurfaces 1\n            -planes 1\n            -lights 1\n            -cameras 1\n            -controlVertices 1\n            -hulls 1\n            -grid 1\n            -imagePlane 1\n            -joints 1\n            -ikHandles 1\n            -deformers 1\n            -dynamics 1\n            -particleInstancers 1\n            -fluids 1\n            -hairSystems 1\n            -follicles 1\n            -nCloths 1\n            -nParticles 1\n            -nRigids 1\n            -dynamicConstraints 1\n            -locators 1\n            -manipulators 1\n"
		+ "            -pluginShapes 1\n            -dimensions 1\n            -handles 1\n            -pivots 1\n            -textures 1\n            -strokes 1\n            -motionTrails 1\n            -clipGhosts 1\n            -bluePencil 1\n            -greasePencils 0\n            -excludeObjectPreset \"All\" \n            -shadows 0\n            -captureSequenceNumber -1\n            -width 911\n            -height 794\n            -sceneRenderFilter 0\n            $editorName;\n        modelEditor -e -viewSelected 0 $editorName;\n\t\tif (!$useSceneConfig) {\n\t\t\tpanel -e -l $label $panelName;\n\t\t}\n\t}\n\n\n\t$panelName = `sceneUIReplacement -getNextPanel \"outlinerPanel\" (localizedPanelLabel(\"ToggledOutliner\")) `;\n\tif (\"\" != $panelName) {\n\t\t$label = `panel -q -label $panelName`;\n\t\toutlinerPanel -edit -l (localizedPanelLabel(\"ToggledOutliner\")) -mbv $menusOkayInPanels  $panelName;\n\t\t$editorName = $panelName;\n        outlinerEditor -e \n            -docTag \"isolOutln_fromSeln\" \n            -showShapes 0\n            -showAssignedMaterials 0\n            -showTimeEditor 1\n"
		+ "            -showReferenceNodes 1\n            -showReferenceMembers 1\n            -showAttributes 0\n            -showConnected 0\n            -showAnimCurvesOnly 0\n            -showMuteInfo 0\n            -organizeByLayer 1\n            -organizeByClip 1\n            -showAnimLayerWeight 1\n            -autoExpandLayers 1\n            -autoExpand 0\n            -showDagOnly 1\n            -showAssets 1\n            -showContainedOnly 1\n            -showPublishedAsConnected 0\n            -showParentContainers 0\n            -showContainerContents 1\n            -ignoreDagHierarchy 0\n            -expandConnections 0\n            -showUpstreamCurves 1\n            -showUnitlessCurves 1\n            -showCompounds 1\n            -showLeafs 1\n            -showNumericAttrsOnly 0\n            -highlightActive 1\n            -autoSelectNewObjects 0\n            -doNotSelectNewObjects 0\n            -dropIsParent 1\n            -transmitFilters 0\n            -setFilter \"defaultSetFilter\" \n            -showSetMembers 1\n            -allowMultiSelection 1\n"
		+ "            -alwaysToggleSelect 0\n            -directSelect 0\n            -isSet 0\n            -isSetMember 0\n            -showUfeItems 1\n            -displayMode \"DAG\" \n            -expandObjects 0\n            -setsIgnoreFilters 1\n            -containersIgnoreFilters 0\n            -editAttrName 0\n            -showAttrValues 0\n            -highlightSecondary 0\n            -showUVAttrsOnly 0\n            -showTextureNodesOnly 0\n            -attrAlphaOrder \"default\" \n            -animLayerFilterOptions \"allAffecting\" \n            -sortOrder \"none\" \n            -longNames 0\n            -niceNames 1\n            -selectCommand \"print(\\\"\\\")\" \n            -showNamespace 1\n            -showPinIcons 0\n            -mapMotionTrails 0\n            -ignoreHiddenAttribute 0\n            -ignoreOutlinerColor 0\n            -renderFilterVisible 0\n            -renderFilterIndex 0\n            -selectionOrder \"chronological\" \n            -expandAttribute 0\n            $editorName;\n\t\tif (!$useSceneConfig) {\n\t\t\tpanel -e -l $label $panelName;\n"
		+ "\t\t}\n\t}\n\n\n\t$panelName = `sceneUIReplacement -getNextPanel \"outlinerPanel\" (localizedPanelLabel(\"Outliner\")) `;\n\tif (\"\" != $panelName) {\n\t\t$label = `panel -q -label $panelName`;\n\t\toutlinerPanel -edit -l (localizedPanelLabel(\"Outliner\")) -mbv $menusOkayInPanels  $panelName;\n\t\t$editorName = $panelName;\n        outlinerEditor -e \n            -showShapes 0\n            -showAssignedMaterials 0\n            -showTimeEditor 1\n            -showReferenceNodes 0\n            -showReferenceMembers 0\n            -showAttributes 0\n            -showConnected 0\n            -showAnimCurvesOnly 0\n            -showMuteInfo 0\n            -organizeByLayer 1\n            -organizeByClip 1\n            -showAnimLayerWeight 1\n            -autoExpandLayers 1\n            -autoExpand 0\n            -showDagOnly 1\n            -showAssets 1\n            -showContainedOnly 1\n            -showPublishedAsConnected 0\n            -showParentContainers 0\n            -showContainerContents 1\n            -ignoreDagHierarchy 0\n            -expandConnections 0\n"
		+ "            -showUpstreamCurves 1\n            -showUnitlessCurves 1\n            -showCompounds 1\n            -showLeafs 1\n            -showNumericAttrsOnly 0\n            -highlightActive 1\n            -autoSelectNewObjects 0\n            -doNotSelectNewObjects 0\n            -dropIsParent 1\n            -transmitFilters 0\n            -setFilter \"defaultSetFilter\" \n            -showSetMembers 1\n            -allowMultiSelection 1\n            -alwaysToggleSelect 0\n            -directSelect 0\n            -showUfeItems 1\n            -displayMode \"DAG\" \n            -expandObjects 0\n            -setsIgnoreFilters 1\n            -containersIgnoreFilters 0\n            -editAttrName 0\n            -showAttrValues 0\n            -highlightSecondary 0\n            -showUVAttrsOnly 0\n            -showTextureNodesOnly 0\n            -attrAlphaOrder \"default\" \n            -animLayerFilterOptions \"allAffecting\" \n            -sortOrder \"none\" \n            -longNames 0\n            -niceNames 1\n            -showNamespace 1\n            -showPinIcons 0\n"
		+ "            -mapMotionTrails 0\n            -ignoreHiddenAttribute 0\n            -ignoreOutlinerColor 0\n            -renderFilterVisible 0\n            $editorName;\n\t\tif (!$useSceneConfig) {\n\t\t\tpanel -e -l $label $panelName;\n\t\t}\n\t}\n\n\n\t$panelName = `sceneUIReplacement -getNextScriptedPanel \"graphEditor\" (localizedPanelLabel(\"Graph Editor\")) `;\n\tif (\"\" != $panelName) {\n\t\t$label = `panel -q -label $panelName`;\n\t\tscriptedPanel -edit -l (localizedPanelLabel(\"Graph Editor\")) -mbv $menusOkayInPanels  $panelName;\n\n\t\t\t$editorName = ($panelName+\"OutlineEd\");\n            outlinerEditor -e \n                -showShapes 1\n                -showAssignedMaterials 0\n                -showTimeEditor 1\n                -showReferenceNodes 0\n                -showReferenceMembers 0\n                -showAttributes 1\n                -showConnected 1\n                -showAnimCurvesOnly 1\n                -showMuteInfo 0\n                -organizeByLayer 1\n                -organizeByClip 1\n                -showAnimLayerWeight 1\n                -autoExpandLayers 1\n"
		+ "                -autoExpand 1\n                -showDagOnly 0\n                -showAssets 1\n                -showContainedOnly 0\n                -showPublishedAsConnected 0\n                -showParentContainers 0\n                -showContainerContents 0\n                -ignoreDagHierarchy 0\n                -expandConnections 1\n                -showUpstreamCurves 1\n                -showUnitlessCurves 1\n                -showCompounds 0\n                -showLeafs 1\n                -showNumericAttrsOnly 1\n                -highlightActive 0\n                -autoSelectNewObjects 1\n                -doNotSelectNewObjects 0\n                -dropIsParent 1\n                -transmitFilters 1\n                -setFilter \"0\" \n                -showSetMembers 0\n                -allowMultiSelection 1\n                -alwaysToggleSelect 0\n                -directSelect 0\n                -showUfeItems 1\n                -displayMode \"DAG\" \n                -expandObjects 0\n                -setsIgnoreFilters 1\n                -containersIgnoreFilters 0\n"
		+ "                -editAttrName 0\n                -showAttrValues 0\n                -highlightSecondary 0\n                -showUVAttrsOnly 0\n                -showTextureNodesOnly 0\n                -attrAlphaOrder \"default\" \n                -animLayerFilterOptions \"allAffecting\" \n                -sortOrder \"none\" \n                -longNames 0\n                -niceNames 1\n                -showNamespace 1\n                -showPinIcons 1\n                -mapMotionTrails 1\n                -ignoreHiddenAttribute 0\n                -ignoreOutlinerColor 0\n                -renderFilterVisible 0\n                $editorName;\n\n\t\t\t$editorName = ($panelName+\"GraphEd\");\n            animCurveEditor -e \n                -displayValues 0\n                -snapTime \"integer\" \n                -snapValue \"none\" \n                -showPlayRangeShades \"on\" \n                -lockPlayRangeShades \"off\" \n                -smoothness \"fine\" \n                -resultSamples 1\n                -resultScreenSamples 0\n                -resultUpdate \"delayed\" \n"
		+ "                -showUpstreamCurves 1\n                -tangentScale 1\n                -tangentLineThickness 1\n                -keyMinScale 1\n                -stackedCurvesMin -1\n                -stackedCurvesMax 1\n                -stackedCurvesSpace 0.2\n                -preSelectionHighlight 0\n                -limitToSelectedCurves 0\n                -constrainDrag 0\n                -valueLinesToggle 0\n                -outliner \"graphEditor1OutlineEd\" \n                -highlightAffectedCurves 0\n                $editorName;\n\t\tif (!$useSceneConfig) {\n\t\t\tpanel -e -l $label $panelName;\n\t\t}\n\t}\n\n\n\t$panelName = `sceneUIReplacement -getNextScriptedPanel \"dopeSheetPanel\" (localizedPanelLabel(\"Dope Sheet\")) `;\n\tif (\"\" != $panelName) {\n\t\t$label = `panel -q -label $panelName`;\n\t\tscriptedPanel -edit -l (localizedPanelLabel(\"Dope Sheet\")) -mbv $menusOkayInPanels  $panelName;\n\n\t\t\t$editorName = ($panelName+\"OutlineEd\");\n            outlinerEditor -e \n                -showShapes 1\n                -showAssignedMaterials 0\n                -showTimeEditor 1\n"
		+ "                -showReferenceNodes 0\n                -showReferenceMembers 0\n                -showAttributes 1\n                -showConnected 1\n                -showAnimCurvesOnly 1\n                -showMuteInfo 0\n                -organizeByLayer 1\n                -organizeByClip 1\n                -showAnimLayerWeight 1\n                -autoExpandLayers 1\n                -autoExpand 1\n                -showDagOnly 0\n                -showAssets 1\n                -showContainedOnly 0\n                -showPublishedAsConnected 0\n                -showParentContainers 0\n                -showContainerContents 0\n                -ignoreDagHierarchy 0\n                -expandConnections 1\n                -showUpstreamCurves 1\n                -showUnitlessCurves 0\n                -showCompounds 0\n                -showLeafs 1\n                -showNumericAttrsOnly 1\n                -highlightActive 0\n                -autoSelectNewObjects 0\n                -doNotSelectNewObjects 1\n                -dropIsParent 1\n                -transmitFilters 0\n"
		+ "                -setFilter \"0\" \n                -showSetMembers 1\n                -allowMultiSelection 1\n                -alwaysToggleSelect 0\n                -directSelect 0\n                -showUfeItems 1\n                -displayMode \"DAG\" \n                -expandObjects 0\n                -setsIgnoreFilters 1\n                -containersIgnoreFilters 0\n                -editAttrName 0\n                -showAttrValues 0\n                -highlightSecondary 0\n                -showUVAttrsOnly 0\n                -showTextureNodesOnly 0\n                -attrAlphaOrder \"default\" \n                -animLayerFilterOptions \"allAffecting\" \n                -sortOrder \"none\" \n                -longNames 0\n                -niceNames 1\n                -showNamespace 1\n                -showPinIcons 0\n                -mapMotionTrails 1\n                -ignoreHiddenAttribute 0\n                -ignoreOutlinerColor 0\n                -renderFilterVisible 0\n                $editorName;\n\n\t\t\t$editorName = ($panelName+\"DopeSheetEd\");\n            dopeSheetEditor -e \n"
		+ "                -displayValues 0\n                -snapTime \"none\" \n                -snapValue \"none\" \n                -outliner \"dopeSheetPanel1OutlineEd\" \n                -hierarchyBelow 0\n                -selectionWindow 0 0 0 0 \n                $editorName;\n\t\tif (!$useSceneConfig) {\n\t\t\tpanel -e -l $label $panelName;\n\t\t}\n\t}\n\n\n\t$panelName = `sceneUIReplacement -getNextScriptedPanel \"timeEditorPanel\" (localizedPanelLabel(\"Time Editor\")) `;\n\tif (\"\" != $panelName) {\n\t\t$label = `panel -q -label $panelName`;\n\t\tscriptedPanel -edit -l (localizedPanelLabel(\"Time Editor\")) -mbv $menusOkayInPanels  $panelName;\n\t\tif (!$useSceneConfig) {\n\t\t\tpanel -e -l $label $panelName;\n\t\t}\n\t}\n\n\n\t$panelName = `sceneUIReplacement -getNextScriptedPanel \"clipEditorPanel\" (localizedPanelLabel(\"Trax Editor\")) `;\n\tif (\"\" != $panelName) {\n\t\t$label = `panel -q -label $panelName`;\n\t\tscriptedPanel -edit -l (localizedPanelLabel(\"Trax Editor\")) -mbv $menusOkayInPanels  $panelName;\n\n\t\t\t$editorName = clipEditorNameFromPanel($panelName);\n            clipEditor -e \n"
		+ "                -displayValues 0\n                -snapTime \"none\" \n                -snapValue \"none\" \n                -initialized 0\n                -manageSequencer 0 \n                $editorName;\n\t\tif (!$useSceneConfig) {\n\t\t\tpanel -e -l $label $panelName;\n\t\t}\n\t}\n\n\n\t$panelName = `sceneUIReplacement -getNextScriptedPanel \"sequenceEditorPanel\" (localizedPanelLabel(\"Camera Sequencer\")) `;\n\tif (\"\" != $panelName) {\n\t\t$label = `panel -q -label $panelName`;\n\t\tscriptedPanel -edit -l (localizedPanelLabel(\"Camera Sequencer\")) -mbv $menusOkayInPanels  $panelName;\n\n\t\t\t$editorName = sequenceEditorNameFromPanel($panelName);\n            clipEditor -e \n                -displayValues 0\n                -snapTime \"none\" \n                -snapValue \"none\" \n                -initialized 0\n                -manageSequencer 1 \n                $editorName;\n\t\tif (!$useSceneConfig) {\n\t\t\tpanel -e -l $label $panelName;\n\t\t}\n\t}\n\n\n\t$panelName = `sceneUIReplacement -getNextScriptedPanel \"hyperGraphPanel\" (localizedPanelLabel(\"Hypergraph Hierarchy\")) `;\n"
		+ "\tif (\"\" != $panelName) {\n\t\t$label = `panel -q -label $panelName`;\n\t\tscriptedPanel -edit -l (localizedPanelLabel(\"Hypergraph Hierarchy\")) -mbv $menusOkayInPanels  $panelName;\n\n\t\t\t$editorName = ($panelName+\"HyperGraphEd\");\n            hyperGraph -e \n                -graphLayoutStyle \"hierarchicalLayout\" \n                -orientation \"horiz\" \n                -mergeConnections 0\n                -zoom 1\n                -animateTransition 0\n                -showRelationships 1\n                -showShapes 0\n                -showDeformers 0\n                -showExpressions 0\n                -showConstraints 0\n                -showConnectionFromSelected 0\n                -showConnectionToSelected 0\n                -showConstraintLabels 0\n                -showUnderworld 0\n                -showInvisible 0\n                -transitionFrames 1\n                -opaqueContainers 0\n                -freeform 0\n                -imagePosition 0 0 \n                -imageScale 1\n                -imageEnabled 0\n                -graphType \"DAG\" \n"
		+ "                -heatMapDisplay 0\n                -updateSelection 1\n                -updateNodeAdded 1\n                -useDrawOverrideColor 0\n                -limitGraphTraversal -1\n                -range 0 0 \n                -iconSize \"smallIcons\" \n                -showCachedConnections 0\n                $editorName;\n\t\tif (!$useSceneConfig) {\n\t\t\tpanel -e -l $label $panelName;\n\t\t}\n\t}\n\n\n\t$panelName = `sceneUIReplacement -getNextScriptedPanel \"hyperShadePanel\" (localizedPanelLabel(\"Hypershade\")) `;\n\tif (\"\" != $panelName) {\n\t\t$label = `panel -q -label $panelName`;\n\t\tscriptedPanel -edit -l (localizedPanelLabel(\"Hypershade\")) -mbv $menusOkayInPanels  $panelName;\n\t\tif (!$useSceneConfig) {\n\t\t\tpanel -e -l $label $panelName;\n\t\t}\n\t}\n\n\n\t$panelName = `sceneUIReplacement -getNextScriptedPanel \"visorPanel\" (localizedPanelLabel(\"Visor\")) `;\n\tif (\"\" != $panelName) {\n\t\t$label = `panel -q -label $panelName`;\n\t\tscriptedPanel -edit -l (localizedPanelLabel(\"Visor\")) -mbv $menusOkayInPanels  $panelName;\n\t\tif (!$useSceneConfig) {\n"
		+ "\t\t\tpanel -e -l $label $panelName;\n\t\t}\n\t}\n\n\n\t$panelName = `sceneUIReplacement -getNextScriptedPanel \"nodeEditorPanel\" (localizedPanelLabel(\"Node Editor\")) `;\n\tif ($nodeEditorPanelVisible || $nodeEditorWorkspaceControlOpen) {\n\t\tif (\"\" == $panelName) {\n\t\t\tif ($useSceneConfig) {\n\t\t\t\t$panelName = `scriptedPanel -unParent  -type \"nodeEditorPanel\" -l (localizedPanelLabel(\"Node Editor\")) -mbv $menusOkayInPanels `;\n\n\t\t\t$editorName = ($panelName+\"NodeEditorEd\");\n            nodeEditor -e \n                -allAttributes 0\n                -allNodes 0\n                -autoSizeNodes 1\n                -consistentNameSize 1\n                -createNodeCommand \"nodeEdCreateNodeCommand\" \n                -connectNodeOnCreation 0\n                -connectOnDrop 0\n                -copyConnectionsOnPaste 0\n                -connectionStyle \"bezier\" \n                -defaultPinnedState 0\n                -additiveGraphingMode 0\n                -connectedGraphingMode 1\n                -settingsChangedCallback \"nodeEdSyncControls\" \n                -traversalDepthLimit -1\n"
		+ "                -keyPressCommand \"nodeEdKeyPressCommand\" \n                -nodeTitleMode \"name\" \n                -gridSnap 0\n                -gridVisibility 1\n                -crosshairOnEdgeDragging 0\n                -popupMenuScript \"nodeEdBuildPanelMenus\" \n                -showNamespace 1\n                -showShapes 1\n                -showSGShapes 0\n                -showTransforms 1\n                -useAssets 1\n                -syncedSelection 1\n                -extendToShapes 1\n                -showUnitConversions 0\n                -editorMode \"default\" \n                -hasWatchpoint 0\n                $editorName;\n\t\t\t}\n\t\t} else {\n\t\t\t$label = `panel -q -label $panelName`;\n\t\t\tscriptedPanel -edit -l (localizedPanelLabel(\"Node Editor\")) -mbv $menusOkayInPanels  $panelName;\n\n\t\t\t$editorName = ($panelName+\"NodeEditorEd\");\n            nodeEditor -e \n                -allAttributes 0\n                -allNodes 0\n                -autoSizeNodes 1\n                -consistentNameSize 1\n                -createNodeCommand \"nodeEdCreateNodeCommand\" \n"
		+ "                -connectNodeOnCreation 0\n                -connectOnDrop 0\n                -copyConnectionsOnPaste 0\n                -connectionStyle \"bezier\" \n                -defaultPinnedState 0\n                -additiveGraphingMode 0\n                -connectedGraphingMode 1\n                -settingsChangedCallback \"nodeEdSyncControls\" \n                -traversalDepthLimit -1\n                -keyPressCommand \"nodeEdKeyPressCommand\" \n                -nodeTitleMode \"name\" \n                -gridSnap 0\n                -gridVisibility 1\n                -crosshairOnEdgeDragging 0\n                -popupMenuScript \"nodeEdBuildPanelMenus\" \n                -showNamespace 1\n                -showShapes 1\n                -showSGShapes 0\n                -showTransforms 1\n                -useAssets 1\n                -syncedSelection 1\n                -extendToShapes 1\n                -showUnitConversions 0\n                -editorMode \"default\" \n                -hasWatchpoint 0\n                $editorName;\n\t\t\tif (!$useSceneConfig) {\n"
		+ "\t\t\t\tpanel -e -l $label $panelName;\n\t\t\t}\n\t\t}\n\t}\n\n\n\t$panelName = `sceneUIReplacement -getNextScriptedPanel \"createNodePanel\" (localizedPanelLabel(\"Create Node\")) `;\n\tif (\"\" != $panelName) {\n\t\t$label = `panel -q -label $panelName`;\n\t\tscriptedPanel -edit -l (localizedPanelLabel(\"Create Node\")) -mbv $menusOkayInPanels  $panelName;\n\t\tif (!$useSceneConfig) {\n\t\t\tpanel -e -l $label $panelName;\n\t\t}\n\t}\n\n\n\t$panelName = `sceneUIReplacement -getNextScriptedPanel \"polyTexturePlacementPanel\" (localizedPanelLabel(\"UV Editor\")) `;\n\tif (\"\" != $panelName) {\n\t\t$label = `panel -q -label $panelName`;\n\t\tscriptedPanel -edit -l (localizedPanelLabel(\"UV Editor\")) -mbv $menusOkayInPanels  $panelName;\n\t\tif (!$useSceneConfig) {\n\t\t\tpanel -e -l $label $panelName;\n\t\t}\n\t}\n\n\n\t$panelName = `sceneUIReplacement -getNextScriptedPanel \"renderWindowPanel\" (localizedPanelLabel(\"Render View\")) `;\n\tif (\"\" != $panelName) {\n\t\t$label = `panel -q -label $panelName`;\n\t\tscriptedPanel -edit -l (localizedPanelLabel(\"Render View\")) -mbv $menusOkayInPanels  $panelName;\n"
		+ "\t\tif (!$useSceneConfig) {\n\t\t\tpanel -e -l $label $panelName;\n\t\t}\n\t}\n\n\n\t$panelName = `sceneUIReplacement -getNextPanel \"shapePanel\" (localizedPanelLabel(\"Shape Editor\")) `;\n\tif (\"\" != $panelName) {\n\t\t$label = `panel -q -label $panelName`;\n\t\tshapePanel -edit -l (localizedPanelLabel(\"Shape Editor\")) -mbv $menusOkayInPanels  $panelName;\n\t\tif (!$useSceneConfig) {\n\t\t\tpanel -e -l $label $panelName;\n\t\t}\n\t}\n\n\n\t$panelName = `sceneUIReplacement -getNextPanel \"posePanel\" (localizedPanelLabel(\"Pose Editor\")) `;\n\tif (\"\" != $panelName) {\n\t\t$label = `panel -q -label $panelName`;\n\t\tposePanel -edit -l (localizedPanelLabel(\"Pose Editor\")) -mbv $menusOkayInPanels  $panelName;\n\t\tif (!$useSceneConfig) {\n\t\t\tpanel -e -l $label $panelName;\n\t\t}\n\t}\n\n\n\t$panelName = `sceneUIReplacement -getNextScriptedPanel \"dynRelEdPanel\" (localizedPanelLabel(\"Dynamic Relationships\")) `;\n\tif (\"\" != $panelName) {\n\t\t$label = `panel -q -label $panelName`;\n\t\tscriptedPanel -edit -l (localizedPanelLabel(\"Dynamic Relationships\")) -mbv $menusOkayInPanels  $panelName;\n"
		+ "\t\tif (!$useSceneConfig) {\n\t\t\tpanel -e -l $label $panelName;\n\t\t}\n\t}\n\n\n\t$panelName = `sceneUIReplacement -getNextScriptedPanel \"relationshipPanel\" (localizedPanelLabel(\"Relationship Editor\")) `;\n\tif (\"\" != $panelName) {\n\t\t$label = `panel -q -label $panelName`;\n\t\tscriptedPanel -edit -l (localizedPanelLabel(\"Relationship Editor\")) -mbv $menusOkayInPanels  $panelName;\n\t\tif (!$useSceneConfig) {\n\t\t\tpanel -e -l $label $panelName;\n\t\t}\n\t}\n\n\n\t$panelName = `sceneUIReplacement -getNextScriptedPanel \"referenceEditorPanel\" (localizedPanelLabel(\"Reference Editor\")) `;\n\tif (\"\" != $panelName) {\n\t\t$label = `panel -q -label $panelName`;\n\t\tscriptedPanel -edit -l (localizedPanelLabel(\"Reference Editor\")) -mbv $menusOkayInPanels  $panelName;\n\t\tif (!$useSceneConfig) {\n\t\t\tpanel -e -l $label $panelName;\n\t\t}\n\t}\n\n\n\t$panelName = `sceneUIReplacement -getNextScriptedPanel \"dynPaintScriptedPanelType\" (localizedPanelLabel(\"Paint Effects\")) `;\n\tif (\"\" != $panelName) {\n\t\t$label = `panel -q -label $panelName`;\n\t\tscriptedPanel -edit -l (localizedPanelLabel(\"Paint Effects\")) -mbv $menusOkayInPanels  $panelName;\n"
		+ "\t\tif (!$useSceneConfig) {\n\t\t\tpanel -e -l $label $panelName;\n\t\t}\n\t}\n\n\n\t$panelName = `sceneUIReplacement -getNextScriptedPanel \"scriptEditorPanel\" (localizedPanelLabel(\"Script Editor\")) `;\n\tif (\"\" != $panelName) {\n\t\t$label = `panel -q -label $panelName`;\n\t\tscriptedPanel -edit -l (localizedPanelLabel(\"Script Editor\")) -mbv $menusOkayInPanels  $panelName;\n\t\tif (!$useSceneConfig) {\n\t\t\tpanel -e -l $label $panelName;\n\t\t}\n\t}\n\n\n\t$panelName = `sceneUIReplacement -getNextScriptedPanel \"profilerPanel\" (localizedPanelLabel(\"Profiler Tool\")) `;\n\tif (\"\" != $panelName) {\n\t\t$label = `panel -q -label $panelName`;\n\t\tscriptedPanel -edit -l (localizedPanelLabel(\"Profiler Tool\")) -mbv $menusOkayInPanels  $panelName;\n\t\tif (!$useSceneConfig) {\n\t\t\tpanel -e -l $label $panelName;\n\t\t}\n\t}\n\n\n\t$panelName = `sceneUIReplacement -getNextScriptedPanel \"contentBrowserPanel\" (localizedPanelLabel(\"Content Browser\")) `;\n\tif (\"\" != $panelName) {\n\t\t$label = `panel -q -label $panelName`;\n\t\tscriptedPanel -edit -l (localizedPanelLabel(\"Content Browser\")) -mbv $menusOkayInPanels  $panelName;\n"
		+ "\t\tif (!$useSceneConfig) {\n\t\t\tpanel -e -l $label $panelName;\n\t\t}\n\t}\n\n\n\tif ($useSceneConfig) {\n        string $configName = `getPanel -cwl (localizedPanelLabel(\"Current Layout\"))`;\n        if (\"\" != $configName) {\n\t\t\tpanelConfiguration -edit -label (localizedPanelLabel(\"Current Layout\")) \n\t\t\t\t-userCreated false\n\t\t\t\t-defaultImage \"vacantCell.xP:/\"\n\t\t\t\t-image \"\"\n\t\t\t\t-sc false\n\t\t\t\t-configString \"global string $gMainPane; paneLayout -e -cn \\\"single\\\" -ps 1 100 100 $gMainPane;\"\n\t\t\t\t-removeAllPanels\n\t\t\t\t-ap false\n\t\t\t\t\t(localizedPanelLabel(\"Persp View\")) \n\t\t\t\t\t\"modelPanel\"\n"
		+ "\t\t\t\t\t\"$panelName = `modelPanel -unParent -l (localizedPanelLabel(\\\"Persp View\\\")) -mbv $menusOkayInPanels `;\\n$editorName = $panelName;\\nmodelEditor -e \\n    -cam `findStartUpCamera persp` \\n    -useInteractiveMode 0\\n    -displayLights \\\"default\\\" \\n    -displayAppearance \\\"smoothShaded\\\" \\n    -activeOnly 0\\n    -ignorePanZoom 0\\n    -wireframeOnShaded 1\\n    -headsUpDisplay 1\\n    -holdOuts 1\\n    -selectionHiliteDisplay 1\\n    -useDefaultMaterial 0\\n    -bufferMode \\\"double\\\" \\n    -twoSidedLighting 0\\n    -backfaceCulling 0\\n    -xray 0\\n    -jointXray 0\\n    -activeComponentsXray 0\\n    -displayTextures 0\\n    -smoothWireframe 0\\n    -lineWidth 1\\n    -textureAnisotropic 0\\n    -textureHilight 1\\n    -textureSampling 2\\n    -textureDisplay \\\"modulate\\\" \\n    -textureMaxSize 32768\\n    -fogging 0\\n    -fogSource \\\"fragment\\\" \\n    -fogMode \\\"linear\\\" \\n    -fogStart 0\\n    -fogEnd 100\\n    -fogDensity 0.1\\n    -fogColor 0.5 0.5 0.5 1 \\n    -depthOfFieldPreview 1\\n    -maxConstantTransparency 1\\n    -rendererName \\\"vp2Renderer\\\" \\n    -objectFilterShowInHUD 1\\n    -isFiltered 0\\n    -colorResolution 256 256 \\n    -bumpResolution 512 512 \\n    -textureCompression 0\\n    -transparencyAlgorithm \\\"frontAndBackCull\\\" \\n    -transpInShadows 0\\n    -cullingOverride \\\"none\\\" \\n    -lowQualityLighting 0\\n    -maximumNumHardwareLights 1\\n    -occlusionCulling 0\\n    -shadingModel 0\\n    -useBaseRenderer 0\\n    -useReducedRenderer 0\\n    -smallObjectCulling 0\\n    -smallObjectThreshold -1 \\n    -interactiveDisableShadows 0\\n    -interactiveBackFaceCull 0\\n    -sortTransparent 1\\n    -controllers 1\\n    -nurbsCurves 1\\n    -nurbsSurfaces 1\\n    -polymeshes 1\\n    -subdivSurfaces 1\\n    -planes 1\\n    -lights 1\\n    -cameras 1\\n    -controlVertices 1\\n    -hulls 1\\n    -grid 1\\n    -imagePlane 1\\n    -joints 1\\n    -ikHandles 1\\n    -deformers 1\\n    -dynamics 1\\n    -particleInstancers 1\\n    -fluids 1\\n    -hairSystems 1\\n    -follicles 1\\n    -nCloths 1\\n    -nParticles 1\\n    -nRigids 1\\n    -dynamicConstraints 1\\n    -locators 1\\n    -manipulators 1\\n    -pluginShapes 1\\n    -dimensions 1\\n    -handles 1\\n    -pivots 1\\n    -textures 1\\n    -strokes 1\\n    -motionTrails 1\\n    -clipGhosts 1\\n    -bluePencil 1\\n    -greasePencils 0\\n    -excludeObjectPreset \\\"All\\\" \\n    -shadows 0\\n    -captureSequenceNumber -1\\n    -width 911\\n    -height 794\\n    -sceneRenderFilter 0\\n    $editorName;\\nmodelEditor -e -viewSelected 0 $editorName\"\n"
		+ "\t\t\t\t\t\"modelPanel -edit -l (localizedPanelLabel(\\\"Persp View\\\")) -mbv $menusOkayInPanels  $panelName;\\n$editorName = $panelName;\\nmodelEditor -e \\n    -cam `findStartUpCamera persp` \\n    -useInteractiveMode 0\\n    -displayLights \\\"default\\\" \\n    -displayAppearance \\\"smoothShaded\\\" \\n    -activeOnly 0\\n    -ignorePanZoom 0\\n    -wireframeOnShaded 1\\n    -headsUpDisplay 1\\n    -holdOuts 1\\n    -selectionHiliteDisplay 1\\n    -useDefaultMaterial 0\\n    -bufferMode \\\"double\\\" \\n    -twoSidedLighting 0\\n    -backfaceCulling 0\\n    -xray 0\\n    -jointXray 0\\n    -activeComponentsXray 0\\n    -displayTextures 0\\n    -smoothWireframe 0\\n    -lineWidth 1\\n    -textureAnisotropic 0\\n    -textureHilight 1\\n    -textureSampling 2\\n    -textureDisplay \\\"modulate\\\" \\n    -textureMaxSize 32768\\n    -fogging 0\\n    -fogSource \\\"fragment\\\" \\n    -fogMode \\\"linear\\\" \\n    -fogStart 0\\n    -fogEnd 100\\n    -fogDensity 0.1\\n    -fogColor 0.5 0.5 0.5 1 \\n    -depthOfFieldPreview 1\\n    -maxConstantTransparency 1\\n    -rendererName \\\"vp2Renderer\\\" \\n    -objectFilterShowInHUD 1\\n    -isFiltered 0\\n    -colorResolution 256 256 \\n    -bumpResolution 512 512 \\n    -textureCompression 0\\n    -transparencyAlgorithm \\\"frontAndBackCull\\\" \\n    -transpInShadows 0\\n    -cullingOverride \\\"none\\\" \\n    -lowQualityLighting 0\\n    -maximumNumHardwareLights 1\\n    -occlusionCulling 0\\n    -shadingModel 0\\n    -useBaseRenderer 0\\n    -useReducedRenderer 0\\n    -smallObjectCulling 0\\n    -smallObjectThreshold -1 \\n    -interactiveDisableShadows 0\\n    -interactiveBackFaceCull 0\\n    -sortTransparent 1\\n    -controllers 1\\n    -nurbsCurves 1\\n    -nurbsSurfaces 1\\n    -polymeshes 1\\n    -subdivSurfaces 1\\n    -planes 1\\n    -lights 1\\n    -cameras 1\\n    -controlVertices 1\\n    -hulls 1\\n    -grid 1\\n    -imagePlane 1\\n    -joints 1\\n    -ikHandles 1\\n    -deformers 1\\n    -dynamics 1\\n    -particleInstancers 1\\n    -fluids 1\\n    -hairSystems 1\\n    -follicles 1\\n    -nCloths 1\\n    -nParticles 1\\n    -nRigids 1\\n    -dynamicConstraints 1\\n    -locators 1\\n    -manipulators 1\\n    -pluginShapes 1\\n    -dimensions 1\\n    -handles 1\\n    -pivots 1\\n    -textures 1\\n    -strokes 1\\n    -motionTrails 1\\n    -clipGhosts 1\\n    -bluePencil 1\\n    -greasePencils 0\\n    -excludeObjectPreset \\\"All\\\" \\n    -shadows 0\\n    -captureSequenceNumber -1\\n    -width 911\\n    -height 794\\n    -sceneRenderFilter 0\\n    $editorName;\\nmodelEditor -e -viewSelected 0 $editorName\"\n"
		+ "\t\t\t\t$configName;\n\n            setNamedPanelLayout (localizedPanelLabel(\"Current Layout\"));\n        }\n\n        panelHistory -e -clear mainPanelHistory;\n        sceneUIReplacement -clear;\n\t}\n\n\ngrid -spacing 5 -size 12 -divisions 5 -displayAxes yes -displayGridLines yes -displayDivisionLines yes -displayPerspectiveLabels no -displayOrthographicLabels no -displayAxesBold yes -perspectiveLabelPosition axis -orthographicLabelPosition edge;\nviewManip -drawCompass 0 -compassAngle 0 -frontParameters \"\" -homeParameters \"\" -selectionLockParameters \"\";\n}\n");
	setAttr ".st" 3;
createNode script -n "sceneConfigurationScriptNode";
	rename -uid "ABB67842-41AA-8100-3688-CD9425009C18";
	setAttr ".b" -type "string" "playbackOptions -min 3 -max 300 -ast 3 -aet 500 ";
	setAttr ".st" 6;
createNode polyPlanarProj -n "polyPlanarProj1";
	rename -uid "B2288B2F-43F7-9D08-56E2-129EDC37A8EE";
	setAttr ".uopa" yes;
	setAttr ".ics" -type "componentList" 1 "f[0:5]";
	setAttr ".ix" -type "matrix" 3.6904568658849213 0 0 0 0 0.57372768141992736 0 0 0 0 6.0603784982722004 0
		 0 0.29756358234490254 0 1;
	setAttr ".ws" yes;
	setAttr ".pc" -type "double3" 0 0.2975635826587677 -7.4505805969238281e-09 ;
	setAttr ".ro" -type "double3" -28.538351698276262 -53.799998839710412 -1.197507220428174e-06 ;
	setAttr ".ps" -type "double2" 7.070089653598151 3.6367701600507116 ;
	setAttr ".per" yes;
	setAttr ".cam" -type "matrix" 1.1483999490737915 0.85900729894638062 0.70892679691314697 0.70891261100769043
		 5.3969174072895676e-17 1.9574348926544189 -0.47775647044181824 -0.47774690389633179
		 1.5690895318984985 -0.62869828939437866 -0.5188559889793396 -0.51884561777114868
		 0.081991910934448242 -0.42306351661682129 15.753771781921387 15.953454971313477;
	setAttr ".prgt" 911;
	setAttr ".ptop" 795;
createNode polyMapCut -n "polyMapCut1";
	rename -uid "0BA9EF2C-4A42-A78D-ACC3-4C9E6B6839D9";
	setAttr ".uopa" yes;
	setAttr ".ics" -type "componentList" 1 "e[8:9]";
createNode polyMapCut -n "polyMapCut2";
	rename -uid "32144D71-4960-66AD-FDD8-3D9D69C5EA19";
	setAttr ".uopa" yes;
	setAttr ".ics" -type "componentList" 3 "e[4]" "e[8]" "e[10]";
createNode polyTweakUV -n "polyTweakUV1";
	rename -uid "BAB02CAC-4350-F202-9F39-84B8CFA99DCC";
	setAttr ".uopa" yes;
	setAttr -s 8 ".uvtk";
	setAttr ".uvtk[1]" -type "float2" -0.27533633 -0.16552904 ;
	setAttr ".uvtk[2]" -type "float2" -0.28410971 -0.23750871 ;
	setAttr ".uvtk[4]" -type "float2" 0.36217493 -0.43577844 ;
	setAttr ".uvtk[5]" -type "float2" 0.67879915 -0.27714393 ;
	setAttr ".uvtk[6]" -type "float2" 0.36014369 -0.37288666 ;
	setAttr ".uvtk[7]" -type "float2" 0.67580146 -0.20141238 ;
	setAttr ".uvtk[8]" -type "float2" 0.66309518 -0.2120102 ;
	setAttr ".uvtk[9]" -type "float2" 0.00062328577 0.087298639 ;
createNode polyMapCut -n "polyMapCut3";
	rename -uid "E18D706C-4428-5770-B008-2DB81F483C60";
	setAttr ".uopa" yes;
	setAttr ".ics" -type "componentList" 1 "e[0:3]";
createNode polyTweakUV -n "polyTweakUV2";
	rename -uid "0400572B-4196-A93A-E6C4-13971F858723";
	setAttr ".uopa" yes;
	setAttr -s 14 ".uvtk[0:13]" -type "float2" -0.72128564 0.2971423 -0.35697407
		 0.18983828 -0.39958227 0.19280748 -0.68402278 0.21353686 -0.34653977 0.639018 -0.31718141
		 0.91399449 -0.30439016 0.63606131 -0.024537086 0.615596 -0.64196056 0.66574323 -0.38146326
		 -0.0853066 -0.27997628 0.90557957 -0.6070652 0.65823466 -0.42831936 -0.076315768
		 -0.091878571 0.1703029;
createNode polySoftEdge -n "polySoftEdge1";
	rename -uid "1815C7CC-423E-FF5E-A98F-12AF11CB7372";
	setAttr ".uopa" yes;
	setAttr ".ics" -type "componentList" 1 "e[*]";
	setAttr ".ix" -type "matrix" 3.6904568658849213 0 0 0 0 0.57372768141992736 0 0 0 0 6.0603784982722004 0
		 0 0.29756358234490254 0 1;
	setAttr ".a" 180;
createNode polySplit -n "polySplit1";
	rename -uid "5B71B967-4096-452D-DBF8-C58A8C4E0AE6";
	setAttr -s 5 ".e[0:4]"  0.0459866 0.95401299 0.95401299 0.0459866
		 0.0459866;
	setAttr -s 5 ".d[0:4]"  -2147483642 -2147483638 -2147483637 -2147483641 -2147483642;
	setAttr ".sma" 180;
	setAttr ".m2015" yes;
createNode polySplit -n "polySplit2";
	rename -uid "08C47189-4428-82A4-46A9-3B9B8FDAE5C4";
	setAttr -s 5 ".e[0:4]"  0.0590459 0.94095403 0.94095403 0.0590459
		 0.0590459;
	setAttr -s 5 ".d[0:4]"  -2147483638 -2147483636 -2147483633 -2147483637 -2147483638;
	setAttr ".sma" 180;
	setAttr ".m2015" yes;
createNode polySplit -n "polySplit3";
	rename -uid "6258395E-4514-0E6D-1B23-80934635AD08";
	setAttr -s 9 ".e[0:8]"  0.103392 0.103392 0.896608 0.103392 0.103392
		 0.103392 0.896608 0.103392 0.103392;
	setAttr -s 9 ".d[0:8]"  -2147483648 -2147483647 -2147483629 -2147483623 -2147483646 -2147483645 
		-2147483621 -2147483631 -2147483648;
	setAttr ".sma" 180;
	setAttr ".m2015" yes;
createNode polySplit -n "polySplit4";
	rename -uid "791573F1-4807-7CBE-0EB1-9985E1E02DAB";
	setAttr -s 9 ".e[0:8]"  0.104113 0.89588702 0.89588702 0.89588702
		 0.104113 0.89588702 0.89588702 0.89588702 0.104113;
	setAttr -s 9 ".d[0:8]"  -2147483629 -2147483619 -2147483620 -2147483613 -2147483621 -2147483615 
		-2147483616 -2147483617 -2147483629;
	setAttr ".sma" 180;
	setAttr ".m2015" yes;
createNode polySplit -n "polySplit5";
	rename -uid "F30947FC-42E0-E82F-1B2B-B582A94B15FD";
	setAttr -s 13 ".e[0:12]"  0.73889202 0.26110801 0.73889202 0.26110801
		 0.26110801 0.73889202 0.26110801 0.26110801 0.73889202 0.73889202 0.26110801 0.73889202
		 0.73889202;
	setAttr -s 13 ".d[0:12]"  -2147483644 -2147483632 -2147483624 -2147483640 -2147483608 -2147483591 
		-2147483639 -2147483622 -2147483630 -2147483643 -2147483595 -2147483612 -2147483644;
	setAttr ".sma" 180;
	setAttr ".m2015" yes;
createNode polySplit -n "polySplit6";
	rename -uid "A5B23DDD-451B-7CDF-819D-84B395424408";
	setAttr -s 13 ".e[0:12]"  0.50419301 0.49580699 0.50419301 0.49580699
		 0.49580699 0.50419301 0.49580699 0.49580699 0.50419301 0.50419301 0.49580699 0.50419301
		 0.50419301;
	setAttr -s 13 ".d[0:12]"  -2147483644 -2147483587 -2147483624 -2147483585 -2147483584 -2147483591 
		-2147483582 -2147483581 -2147483630 -2147483643 -2147483578 -2147483612 -2147483644;
	setAttr ".sma" 180;
	setAttr ".m2015" yes;
createNode polyCube -n "polyCube2";
	rename -uid "01F96B08-424E-7363-B982-ED8ACC910A89";
	setAttr ".cuv" 4;
createNode polySplit -n "polySplit7";
	rename -uid "CB5DF580-47A3-5764-6F13-59A95F85A8E4";
	setAttr -s 5 ".e[0:4]"  0.91017598 0.91017598 0.91017598 0.91017598
		 0.91017598;
	setAttr -s 5 ".d[0:4]"  -2147483648 -2147483647 -2147483646 -2147483645 -2147483648;
	setAttr ".sma" 180;
	setAttr ".m2015" yes;
createNode polySplit -n "polySplit8";
	rename -uid "62C92CBB-49A9-8648-7A95-D0A14742902B";
	setAttr -s 5 ".e[0:4]"  0.089414597 0.089414597 0.089414597 0.089414597
		 0.089414597;
	setAttr -s 5 ".d[0:4]"  -2147483648 -2147483647 -2147483646 -2147483645 -2147483648;
	setAttr ".sma" 180;
	setAttr ".m2015" yes;
createNode polySplit -n "polySplit9";
	rename -uid "8A18EDB4-48FA-85F7-61BB-D9A808D901B9";
	setAttr -s 9 ".e[0:8]"  0.903189 0.096811302 0.096811302 0.096811302
		 0.096811302 0.903189 0.903189 0.903189 0.903189;
	setAttr -s 9 ".d[0:8]"  -2147483642 -2147483638 -2147483621 -2147483629 -2147483637 -2147483641 
		-2147483631 -2147483623 -2147483642;
	setAttr ".sma" 180;
	setAttr ".m2015" yes;
createNode polySplit -n "polySplit10";
	rename -uid "298B0344-4A6C-4032-357E-EF8F7E2C214E";
	setAttr -s 9 ".e[0:8]"  0.0984108 0.90158898 0.90158898 0.90158898
		 0.90158898 0.0984108 0.0984108 0.0984108 0.0984108;
	setAttr -s 9 ".d[0:8]"  -2147483642 -2147483619 -2147483618 -2147483617 -2147483616 -2147483641 
		-2147483631 -2147483623 -2147483642;
	setAttr ".sma" 180;
	setAttr ".m2015" yes;
createNode polySplit -n "polySplit11";
	rename -uid "DB9653D5-48E5-E64A-F8FB-78AA84D6120B";
	setAttr -s 13 ".e[0:12]"  0.47473201 0.52526802 0.52526802 0.52526802
		 0.52526802 0.52526802 0.52526802 0.47473201 0.47473201 0.47473201 0.47473201 0.47473201
		 0.47473201;
	setAttr -s 13 ".d[0:12]"  -2147483644 -2147483596 -2147483612 -2147483640 -2147483622 -2147483630 
		-2147483639 -2147483608 -2147483592 -2147483643 -2147483632 -2147483624 -2147483644;
	setAttr ".sma" 180;
	setAttr ".m2015" yes;
createNode polyPlanarProj -n "polyPlanarProj2";
	rename -uid "B5DACEF0-46AB-869B-2E87-93AD41DFC504";
	setAttr ".uopa" yes;
	setAttr ".ics" -type "componentList" 1 "f[0:41]";
	setAttr ".ix" -type "matrix" 1.4002473749445541 0 0 0 0 0.056893799540346138 0 0
		 0 0 1.4002473749445541 0 0.53128309571122112 0.61201423654973197 1.6763871674724689 1;
	setAttr ".ws" yes;
	setAttr ".pc" -type "double3" 0.53128308057785034 0.61201423406600952 1.6763873100280762 ;
	setAttr ".ro" -type "double3" -19.538351946747262 -43.399999827550921 -5.3851156776363582e-07 ;
	setAttr ".ps" -type "double2" 1.979476758922976 0.71562950138625236 ;
	setAttr ".per" yes;
	setAttr ".cam" -type "matrix" 1.4127840995788574 0.51200497150421143 0.64753645658493042 0.64752352237701416
		 -5.7076838694698198e-17 2.0998597145080566 -0.33444446325302124 -0.33443775773048401
		 1.3360035419464111 -0.54143005609512329 -0.68475061655044556 -0.68473690748214722
		 -3.313058614730835 -0.11164849996566772 3.542525053024292 3.7424521446228027;
	setAttr ".prgt" 911;
	setAttr ".ptop" 795;
createNode polyMapCut -n "polyMapCut4";
	rename -uid "3140F199-4C5D-A489-02E7-369AA86646AE";
	setAttr ".uopa" yes;
	setAttr ".ics" -type "componentList" 6 "e[1:2]" "e[7]" "e[13:14]" "e[21:22]" "e[33]" "e[49]";
createNode polyMapCut -n "polyMapCut5";
	rename -uid "05D852A7-4277-C5BE-B273-C8B382E99FDE";
	setAttr ".uopa" yes;
	setAttr ".ics" -type "componentList" 8 "e[1:2]" "e[6:7]" "e[13:14]" "e[21:22]" "e[28]" "e[33]" "e[44]" "e[49]";
createNode polyTweakUV -n "polyTweakUV3";
	rename -uid "15734195-4102-9868-D1EE-D4AAD4B43D9B";
	setAttr ".uopa" yes;
	setAttr -s 56 ".uvtk[0:55]" -type "float2" 0 0.30256054 -0.089158326
		 0.2524136 -0.079857349 0.23111033 0.0037598014 0.2878719 0.012158126 0.27181977 -0.069617987
		 0.20735466 0.32763466 0.24739757 0.37983176 0.23124541 -0.036391594 0.2383846 -0.11130357
		 0.19714597 -0.10441475 0.20695928 -0.035910692 0.2461659 0 0.30256054 -0.075282246
		 0.2430124 -0.72650325 -0.32434726 -0.70718396 -0.3295612 -0.71082306 -0.35121009
		 -0.72523785 -0.34279186 0.004329294 0.29314902 -0.66874152 -0.29630551 -0.67596018
		 -0.27953145 -0.71486461 -0.37536561 0.0025323778 0.47213316 -0.66067994 -0.3150335
		 -0.72280157 -0.36535442 -0.65259111 -0.24352264 -0.66108543 -0.25139114 -0.70001471
		 -0.29854339 -0.69509798 -0.28564024 -0.65970933 -0.27855831 -0.02450512 0.25256124
		 -0.020314921 0.24784735 -0.090949729 0.20566812 -0.64403284 -0.24859005 -0.69351876
		 -0.29486111 -0.69646966 -0.31372121 0.023073748 0.54295182 0.030862227 0.53335392
		 -0.69978881 -0.33473286 0.0022263974 0.57619393 0.31456536 0.25257713 0.32763463
		 0.24739759 -0.015660793 0.24262306 -0.037956532 0.2317321 -0.017499432 0.50094283
		 0.0098534375 0.56627321 -0.70741856 -0.31392685 -0.11900999 0.18625128 -0.67058682
		 -0.26015443 0.30962327 0.25458989 -0.01443325 0.46833539 -0.033890203 0.49645656
		 0.0089702904 0.28272942 0.3702116 0.22164223 0.35608467 0.22617424 0.36284289 0.2404097;
createNode polyMapSew -n "polyMapSew1";
	rename -uid "A56253D3-49D5-47CD-4370-EF9A939E68F9";
	setAttr ".uopa" yes;
	setAttr ".ics" -type "componentList" 1 "e[44]";
createNode polyTweakUV -n "polyTweakUV4";
	rename -uid "6F7EE7B9-4ADC-2C9F-836D-CE962FD94EDE";
	setAttr ".uopa" yes;
	setAttr -s 54 ".uvtk[0:53]" -type "float2" 0.56311738 -0.054470699 0.57803339
		 -0.047694486 0.57388037 -0.044860028 0.56057793 -0.056877937 0.55563021 -0.058622785
		 0.56930619 -0.041787028 0.19506344 -0.055743344 0.39447805 -0.064940736 0.78550476
		 -0.20370747 0.80021316 -0.1897926 0.79614359 -0.18704566 0.7850008 -0.20060024 0.58637279
		 -0.081853218 0.59761649 -0.062145784 0.70772785 0.14490265 0.72535396 0.13655631
		 0.72786725 0.14077482 0.70705533 0.14796111 0.58328843 -0.085957177 0.6909343 0.13270925
		 0.69495094 0.12988842 0.73066783 0.14549778 0.052386332 -0.22386776 0.68645549 0.13587205
		 0.70605278 0.15256335 0.90951741 -0.0073827761 0.91374993 -0.0098715536 0.9208414
		 0.011217011 0.91780239 0.01044021 0.71345377 0.11738357 0.7652387 -0.19159868 0.76278627
		 -0.19588669 0.77571809 -0.1738876 0.88883221 0.0053322604 0.90124625 0.02418882 0.90401596
		 0.02824739 0.23711897 -0.38074625 0.21749048 -0.36408481 0.90712106 0.032772299 0.22167921
		 -0.39894122 0.37977532 -0.21281353 0.36017191 -0.19615963 0.56712317 -0.19019203
		 0.78632581 -0.20837066 0.036921348 -0.24208689 0.20202719 -0.38229483 0.92538017
		 0.012350059 0.80475831 -0.19287589 0.91848552 -0.012643238 0.1770187 -0.040484481
		 0.034365401 -0.20856445 0.01890859 -0.2267805 0.39370167 -0.19632545 0.19077572 -0.024067115;
createNode polyTweakUV -n "polyTweakUV5";
	rename -uid "6B23BA96-42CA-3EB9-3414-FDB42143A422";
	setAttr ".uopa" yes;
	setAttr -s 76 ".uvtk[0:75]" -type "float2" 0.39045888 -0.1475119 0.22584282
		 -0.12351243 0.24536957 -0.12635539 0.37095645 -0.14466858 0.21529147 -0.33260617
		 0.1969244 -0.45828319 0.19576107 -0.32975498 0.070169821 -0.31143028 0.36041531 -0.35375905
		 0.24408057 0.0021199994 0.1773867 -0.45543 0.34089011 -0.35091403 0.26361227 -0.00071943342
		 0.10025907 -0.10519092 0.36957383 -0.15415309 0.38907725 -0.15699656 0.098875374
		 -0.11467525 0.22445953 -0.13299687 0.2439864 -0.13584016 0.36210769 -0.342141 0.071864769
		 -0.29981264 0.34258375 -0.33929613 0.21698581 -0.32098785 0.19745563 -0.31813717
		 0.24219495 -0.010869402 0.11324345 -0.10708524 0.35797176 -0.14277513 0.26172608
		 -0.01370917 0.35658908 -0.15225968 0.32959792 -0.33740321 0.19882341 -0.44528911
		 0.32790419 -0.34902114 0.083154932 -0.31332493 0.17928645 -0.44243613 0.084849879
		 -0.30170733 0.11185975 -0.11656956 0.2557098 -0.13754961 0.25709295 -0.1280649 0.24707253
		 -0.11462744 0.22754534 -0.11178479 0.21411976 -0.12180216 0.21273643 -0.13128659
		 0.1857319 -0.31642658 0.18403731 -0.32804435 0.19404584 -0.34148657 0.21357694 -0.34433794
		 0.22701596 -0.33431512 0.22871023 -0.32269692 0.37604865 -0.14541097 0.25851241 2.1969083e-05
		 0.3746663 -0.15489553 0.34768161 -0.34003896 0.3459883 -0.35165685 0.19182289 -0.45753819
		 0.19372216 -0.44454408 0.20847723 -0.34359345 0.21019198 -0.3318617 0.2118863 -0.32024345
		 0.23888776 -0.13509776 0.24027102 -0.125613 0.24197385 -0.11388521 0.25662643 -0.01296772
		 0.38319334 -0.14645265 0.25135699 0.0010621863 0.38181138 -0.15593722 0.35483417
		 -0.34108114 0.35314134 -0.35269916 0.18466537 -0.45649302 0.18656485 -0.44349903
		 0.20132205 -0.34254888 0.20303701 -0.33081716 0.20473149 -0.31919912 0.23173419 -0.13405612
		 0.23311743 -0.12457154 0.23482011 -0.11284383 0.24947116 -0.011927322;
createNode polyCube -n "polyCube3";
	rename -uid "70F4EE35-468D-B4B8-1BD2-20B23C8A1C77";
	setAttr ".cuv" 4;
createNode polyPlanarProj -n "polyPlanarProj3";
	rename -uid "7F8D3F52-4F3F-F62B-F008-51B4510480B0";
	setAttr ".uopa" yes;
	setAttr ".ics" -type "componentList" 1 "f[0:5]";
	setAttr ".ix" -type "matrix" 6.4376668506678785 0 0 0 0 0.10566578697485401 0 0 0 0 8.5383516679350535 0
		 0 0.06385791705916799 0.33244645588015886 1;
	setAttr ".ws" yes;
	setAttr ".pc" -type "double3" -1.4901161193847656e-08 0.063857913017272949 0.33244645595550537 ;
	setAttr ".ro" -type "double3" -29.138354023035369 -46.600001309922725 1.5034171046504841e-06 ;
	setAttr ".ps" -type "double2" 10.626990574669847 5.2263993041366952 ;
	setAttr ".per" yes;
	setAttr ".cam" -type "matrix" 1.3360035419464111 0.78828775882720947 0.63463675975799561 0.63462406396865845
		 5.3969174072895676e-17 1.9461803436279297 -0.48692989349365234 -0.48692014813423157
		 1.4127840995788574 -0.74544668197631836 -0.60014617443084717 -0.60013419389724731
		 -2.9103858470916748 0.26998308300971985 17.809392929077148 18.009035110473633;
	setAttr ".prgt" 911;
	setAttr ".ptop" 795;
createNode polyMapCut -n "polyMapCut6";
	rename -uid "CEFC7269-4644-0DD2-769E-7FBF2392E080";
	setAttr ".uopa" yes;
	setAttr ".ics" -type "componentList" 1 "e[4:5]";
createNode polyMapCut -n "polyMapCut7";
	rename -uid "70DA1A89-4085-D62A-BEA3-EBA1BDC4B091";
	setAttr ".uopa" yes;
	setAttr ".ics" -type "componentList" 1 "e[1]";
createNode polyMapCut -n "polyMapCut8";
	rename -uid "45A42D75-408F-48AC-75AE-F3B57446E1BD";
	setAttr ".uopa" yes;
	setAttr ".ics" -type "componentList" 1 "e[1]";
createNode polyMapCut -n "polyMapCut9";
	rename -uid "40C4FD79-4287-D25A-B715-6CA747FE23B5";
	setAttr ".uopa" yes;
	setAttr ".ics" -type "componentList" 2 "e[1]" "e[4:5]";
createNode polyMapCut -n "polyMapCut10";
	rename -uid "D05858AB-4728-667D-8E06-27977D150B68";
	setAttr ".uopa" yes;
	setAttr ".ics" -type "componentList" 1 "e[6:7]";
createNode polyTweakUV -n "polyTweakUV6";
	rename -uid "5019C566-44DA-5770-D67C-CD900630905A";
	setAttr ".uopa" yes;
	setAttr -s 12 ".uvtk[0:11]" -type "float2" 0.18853812 0.45079038 -0.25285795
		 -0.083741218 -0.25405368 -0.096537858 -0.44375911 0.43415383 -0.01043155 -0.31389627
		 0.42929202 0.088069916 -0.0091450736 -0.30430189 0.42933089 0.094453454 -0.25015754
		 -0.092663258 0.1926 0.44025663 -0.88275075 -0.10025111 0.18853812 0.45079038;
createNode polyMapCut -n "polyMapCut11";
	rename -uid "E0B8FA8A-46AB-26AB-515C-2CB4A3C2B61F";
	setAttr ".uopa" yes;
	setAttr ".ics" -type "componentList" 1 "e[8:9]";
createNode polyTweakUV -n "polyTweakUV7";
	rename -uid "2CEF454D-4109-1A40-0514-D795E8E9BD3B";
	setAttr ".uopa" yes;
	setAttr -s 14 ".uvtk[0:13]" -type "float2" -0.36438537 -0.43520117 -0.7276181
		 -0.2051027 -0.73356819 -0.20133096 0.25146469 0.54064953 -0.42832667 0.2795479 -0.055891853
		 0.054308064 -0.42401597 0.27743861 -0.057521194 0.050533198 -0.73139358 -0.21107139
		 -0.36816096 -0.44116253 -0.11525722 0.77242547 -0.35811558 -0.44562465 -0.42245561
		 0.28561446 -0.04981982 0.048453398;
createNode polyLayoutUV -n "polyLayoutUV1";
	rename -uid "49162BF4-4079-AE2A-4B18-B9949690DC3C";
	setAttr ".uopa" yes;
	setAttr ".ics" -type "componentList" 1 "f[*]";
	setAttr ".fr" no;
	setAttr ".l" 0;
	setAttr ".ps" 0.20000000298023224;
	setAttr ".sc" 0;
	setAttr ".dl" yes;
	setAttr ".rbf" 3;
	setAttr ".lm" 1;
createNode polyTweakUV -n "polyTweakUV8";
	rename -uid "6931F2B6-4AEC-E35E-577C-B8BAFE5DEB56";
	setAttr ".uopa" yes;
	setAttr -s 76 ".uvtk[0:75]" -type "float2" -0.10528404 -0.075448595
		 0.088582188 0.11833481 0.065590896 0.095344447 -0.082316212 -0.052490711 -0.17719591
		 0.33825493 -0.32509691 0.48632196 -0.15419009 0.36124203 -0.0062634968 0.50907171
		 -0.34810007 0.16741264 0.23655193 -0.029517377 -0.30208135 0.50931686 -0.32510749
		 0.19039895 0.21356037 -0.052517779 0.23649718 0.26615781 -0.093481325 -0.041321013
		 -0.11645041 -0.064280115 0.22533333 0.2773287 0.077417754 0.12950543 0.054425951
		 0.10651517 -0.33442211 0.15373214 0.0074113216 0.4953883 -0.3114309 0.17671679 -0.16351962
		 0.32457173 -0.1405146 0.34755901 0.22125307 -0.014230642 0.221204 0.2508741 -0.067023881
		 -0.037205767 0.19826144 -0.037230026 -0.078188911 -0.026035987 -0.29613808 0.19200382
		 -0.30980515 0.47101304 -0.30981463 0.20568602 -0.021557946 0.49378732 -0.28679067
		 0.49400711 -0.0078829583 0.48010388 0.21003999 0.26204494 0.040618919 0.092714854
		 0.051783945 0.08154428 0.079403602 0.081541777 0.1023949 0.10453314 0.10238973 0.1321338
		 0.091225468 0.14330454 -0.12670596 0.36135861 -0.14038137 0.37504178 -0.16799556
		 0.37506458 -0.19100222 0.3520768 -0.1910032 0.32445291 -0.17732686 0.31076974 -0.088313341
		 -0.058485221 0.2195636 -0.04651216 -0.099478789 -0.047315862 -0.3174341 0.17071535
		 -0.33111098 0.18439703 -0.31908748 0.49232608 -0.30379596 0.4770171 -0.18499501 0.35807914
		 -0.17118885 0.34425706 -0.15751292 0.33057395 0.060429264 0.11251804 0.071594119
		 0.10134755 0.085406832 0.087545007 0.20426483 -0.031224653 -0.096727535 -0.066895805
		 0.22798645 -0.038086064 -0.10789341 -0.055726826 -0.32585689 0.16229494 -0.33953431
		 0.1759761 -0.31065562 0.50075018 -0.29536459 0.48544079 -0.17656654 0.36650062 -0.16276079
		 0.35267833 -0.14908504 0.33899534 0.068852276 0.1209406 0.080016881 0.10976993 0.093829758
		 0.095967844 0.21268767 -0.022798888;
createNode polyLayoutUV -n "polyLayoutUV2";
	rename -uid "285DAE8C-43ED-84BA-C399-3AA50F76EC3C";
	setAttr ".uopa" yes;
	setAttr ".ics" -type "componentList" 1 "f[*]";
	setAttr ".fr" no;
	setAttr ".l" 0;
	setAttr ".ps" 0.20000000298023224;
	setAttr ".sc" 0;
	setAttr ".dl" yes;
	setAttr ".rbf" 3;
	setAttr ".lm" 1;
createNode aiOptions -s -n "defaultArnoldRenderOptions";
	rename -uid "156D4678-417B-451C-9A74-5CA77C2E2248";
	setAttr ".version" -type "string" "5.4.5";
createNode aiAOVFilter -s -n "defaultArnoldFilter";
	rename -uid "752B8DFC-4D73-D74E-DD55-F5B8CA30CA5F";
createNode aiAOVDriver -s -n "defaultArnoldDriver";
	rename -uid "74AFB26B-49CE-79F3-B851-FEBE122CE6AB";
createNode aiAOVDriver -s -n "defaultArnoldDisplayDriver";
	rename -uid "7AD9D8EC-40E6-9A11-3B8A-04A48B1EB2D7";
	setAttr ".ai_translator" -type "string" "maya";
	setAttr ".output_mode" 0;
createNode aiImagerDenoiserOidn -s -n "defaultArnoldDenoiser";
	rename -uid "3FB649D9-42F1-DACC-2494-9C893E5E6128";
createNode polyLayoutUV -n "polyLayoutUV3";
	rename -uid "2924ADEC-4E21-CB99-C03E-1B946812A50F";
	setAttr ".uopa" yes;
	setAttr ".ics" -type "componentList" 1 "f[*]";
	setAttr ".fr" no;
	setAttr ".l" 0;
	setAttr ".ps" 0.20000000298023224;
	setAttr ".sc" 0;
	setAttr ".dl" yes;
	setAttr ".rbf" 3;
	setAttr ".lm" 1;
createNode polyLayoutUV -n "polyLayoutUV4";
	rename -uid "CE7F8B4E-43E7-A948-FD36-2988F69E0FB7";
	setAttr ".uopa" yes;
	setAttr ".ics" -type "componentList" 1 "f[*]";
	setAttr ".fr" no;
	setAttr ".l" 0;
	setAttr ".ps" 0.20000000298023224;
	setAttr ".sc" 0;
	setAttr ".dl" yes;
	setAttr ".rbf" 3;
	setAttr ".lm" 1;
createNode polyTweakUV -n "polyTweakUV9";
	rename -uid "DFA8D5BE-45D6-730A-96A4-BBA3FB00CD30";
	setAttr ".uopa" yes;
	setAttr -s 54 ".uvtk[0:53]" -type "float2" -0.35053247 -0.26638949 -0.35753942
		 -0.28589213 -0.35309824 -0.28588009 -0.35011783 -0.26457566 -0.34773147 -0.26127881
		 -0.34821653 -0.28590524 -0.38066572 -0.24111965 -0.38040289 -0.25886711 -0.57794493
		 -0.27103752 -0.58220816 -0.28776398 -0.57802635 -0.28787565 -0.57617074 -0.27304459
		 -0.38064107 -0.2682839 -0.37889057 -0.28614521 -0.36184549 -0.48575771 -0.37844157
		 -0.48780605 -0.37831596 -0.49200109 -0.35995445 -0.48766696 -0.3804715 -0.26381874
		 -0.35524878 -0.46917549 -0.35947093 -0.46899563 -0.37816373 -0.49668789 -0.38325277
		 -0.060153063 -0.35052982 -0.46938923 -0.35711107 -0.49054214 -0.57711041 -0.46887636
		 -0.5812934 -0.46902478 -0.57676351 -0.4874492 -0.57492876 -0.48549584 -0.37867174
		 -0.46852523 -0.55786431 -0.27054924 -0.55805212 -0.26636714 -0.55733335 -0.28799304
		 -0.55639201 -0.46854126 -0.55667275 -0.48780087 -0.55680263 -0.49199229 -0.58228165
		 -0.063030921 -0.56113136 -0.062731549 -0.55695838 -0.49667549 -0.58253759 -0.043389548
		 -0.57969773 -0.24418384 -0.55853653 -0.24388336 -0.55827039 -0.26169613 -0.58065438
		 -0.26805443 -0.383533 -0.040537231 -0.56141037 -0.04310726 -0.57951528 -0.49034813
		 -0.58688599 -0.28762791 -0.58596957 -0.46920371 -0.36126018 -0.24084227 -0.36383832
		 -0.059874281 -0.36409658 -0.040263511 -0.57944816 -0.26197496 -0.36101106 -0.25859696;
createNode polyTweakUV -n "polyTweakUV10";
	rename -uid "D981DC2A-4CEB-ABE8-4D70-64920F181CEC";
	setAttr ".uopa" yes;
	setAttr -s 54 ".uvtk[0:53]" -type "float2" -0.0017156765 0.2283984 -0.0087227104
		 0.20889583 -0.0042815879 0.20890769 -0.0013012564 0.23021208 0.0010852342 0.23350906
		 0.00060004718 0.20888272 -0.031849127 0.25366816 -0.031586252 0.23592064 -0.22912838
		 0.22375035 -0.23339164 0.20702381 -0.22920983 0.20691222 -0.22735414 0.22174333 -0.031824537
		 0.226504 -0.030074056 0.20864268 -0.013028864 0.009030181 -0.02962492 0.0069818436
		 -0.029499354 0.0027868571 -0.011137797 0.0071210009 -0.031654961 0.23096913 -0.0064321887
		 0.025612313 -0.010654279 0.02579229 -0.029347083 -0.0019000592 -0.034436177 0.43463469
		 -0.0017132286 0.025398614 -0.0082945479 0.0042457692 -0.22829387 0.025911458 -0.23247685
		 0.025762977 -0.22794692 0.0073386291 -0.22611216 0.0092919953 -0.029855136 0.026262691
		 -0.20904778 0.22423871 -0.20923553 0.22842073 -0.20851681 0.20679487 -0.20757541
		 0.02624668 -0.20785619 0.0069869687 -0.20798604 0.0027956108 -0.23346508 0.43175682
		 -0.21231478 0.43205619 -0.20814188 -0.001887614 -0.23372105 0.45139816 -0.23088108
		 0.25060388 -0.20971984 0.25090447 -0.20945369 0.23309167 -0.23183782 0.22673343 -0.034716465
		 0.45425034 -0.21259379 0.45168057 -0.23069876 0.0044396515 -0.23806943 0.20715982
		 -0.23715302 0.025584102 -0.012443535 0.25394553 -0.015021686 0.43491349 -0.015279829
		 0.45452431 -0.23063146 0.23281293 -0.012194515 0.23619099;
createNode polyLayoutUV -n "polyLayoutUV5";
	rename -uid "176940B0-4295-F2C7-DCD1-3AB636CD326B";
	setAttr ".uopa" yes;
	setAttr ".ics" -type "componentList" 1 "f[*]";
	setAttr ".fr" no;
	setAttr ".l" 0;
	setAttr ".ps" 0.20000000298023224;
	setAttr ".sc" 0;
	setAttr ".dl" yes;
	setAttr ".rbf" 3;
	setAttr ".lm" 1;
createNode polyTweakUV -n "polyTweakUV11";
	rename -uid "92391A08-41CA-CF74-1277-54BF132C74D2";
	setAttr ".uopa" yes;
	setAttr -s 14 ".uvtk[0:13]" -type "float2" 0.001935199 -0.0042906133
		 -0.0055033793 -0.0042911726 -0.0056254864 -0.0042911554 0.0019374019 0.015551557
		 -0.0056232098 0.005575886 0.0019338492 0.0056963125 -0.0055011092 0.0055758357 0.0019339144
		 0.0055743512 -0.0055033783 -0.0044132848 0.0019352271 -0.0044126953 -0.0054954751
		 0.015554955 0.0020597642 -0.0042906115 -0.0055009751 0.005697906 0.0020559866 0.0055744108;
select -ne :time1;
	setAttr ".o" 3;
	setAttr ".unw" 3;
select -ne :hardwareRenderingGlobals;
	setAttr ".otfna" -type "stringArray" 22 "NURBS Curves" "NURBS Surfaces" "Polygons" "Subdiv Surface" "Particles" "Particle Instance" "Fluids" "Strokes" "Image Planes" "UI" "Lights" "Cameras" "Locators" "Joints" "IK Handles" "Deformers" "Motion Trails" "Components" "Hair Systems" "Follicles" "Misc. UI" "Ornaments"  ;
	setAttr ".otfva" -type "Int32Array" 22 0 1 1 1 1 1
		 1 1 1 0 0 0 0 0 0 0 0 0
		 0 0 0 0 ;
	setAttr ".fprt" yes;
	setAttr ".rtfm" 1;
select -ne :renderPartition;
	setAttr -s 2 ".st";
select -ne :renderGlobalsList1;
select -ne :defaultShaderList1;
	setAttr -s 5 ".s";
select -ne :postProcessList1;
	setAttr -s 2 ".p";
select -ne :defaultRenderingList1;
select -ne :standardSurface1;
	setAttr ".bc" -type "float3" 0.40000001 0.40000001 0.40000001 ;
	setAttr ".sr" 0.5;
select -ne :initialShadingGroup;
	setAttr -s 4 ".dsm";
	setAttr ".ro" yes;
select -ne :initialParticleSE;
	setAttr ".ro" yes;
select -ne :defaultRenderGlobals;
	addAttr -ci true -h true -sn "dss" -ln "defaultSurfaceShader" -dt "string";
	setAttr ".dss" -type "string" "standardSurface1";
select -ne :defaultResolution;
	setAttr ".pa" 1;
select -ne :defaultColorMgtGlobals;
	setAttr ".cfe" yes;
	setAttr ".cfp" -type "string" "<MAYA_RESOURCES>/OCIO-configs/Maya2022-default/config.ocio";
	setAttr ".vtn" -type "string" "ACES 1.0 SDR-video (sRGB)";
	setAttr ".vn" -type "string" "ACES 1.0 SDR-video";
	setAttr ".dn" -type "string" "sRGB";
	setAttr ".wsn" -type "string" "ACEScg";
	setAttr ".otn" -type "string" "ACES 1.0 SDR-video (sRGB)";
	setAttr ".potn" -type "string" "ACES 1.0 SDR-video (sRGB)";
select -ne :hardwareRenderGlobals;
	setAttr ".ctrs" 256;
	setAttr ".btrs" 512;
connectAttr "polyLayoutUV2.out" "pCubeShape1.i";
connectAttr "polyTweakUV8.uvtk[0]" "pCubeShape1.uvst[0].uvtw";
connectAttr "polyTweakUV10.out" "pCubeShape2.i";
connectAttr "polyTweakUV10.uvtk[0]" "pCubeShape2.uvst[0].uvtw";
connectAttr "polyTweakUV9.out" "pCubeShape3.i";
connectAttr "polyTweakUV9.uvtk[0]" "pCubeShape3.uvst[0].uvtw";
connectAttr "polyTweakUV11.out" "pCubeShape4.i";
connectAttr "polyTweakUV11.uvtk[0]" "pCubeShape4.uvst[0].uvtw";
relationship "link" ":lightLinker1" ":initialShadingGroup.message" ":defaultLightSet.message";
relationship "link" ":lightLinker1" ":initialParticleSE.message" ":defaultLightSet.message";
relationship "shadowLink" ":lightLinker1" ":initialShadingGroup.message" ":defaultLightSet.message";
relationship "shadowLink" ":lightLinker1" ":initialParticleSE.message" ":defaultLightSet.message";
connectAttr "layerManager.dli[0]" "defaultLayer.id";
connectAttr "renderLayerManager.rlmi[0]" "defaultRenderLayer.rlid";
connectAttr "polyCube1.out" "polyPlanarProj1.ip";
connectAttr "pCubeShape1.wm" "polyPlanarProj1.mp";
connectAttr "polyPlanarProj1.out" "polyMapCut1.ip";
connectAttr "polyMapCut1.out" "polyMapCut2.ip";
connectAttr "polyMapCut2.out" "polyTweakUV1.ip";
connectAttr "polyTweakUV1.out" "polyMapCut3.ip";
connectAttr "polyMapCut3.out" "polyTweakUV2.ip";
connectAttr "polyTweakUV2.out" "polySoftEdge1.ip";
connectAttr "pCubeShape1.wm" "polySoftEdge1.mp";
connectAttr "polySoftEdge1.out" "polySplit1.ip";
connectAttr "polySplit1.out" "polySplit2.ip";
connectAttr "polySplit2.out" "polySplit3.ip";
connectAttr "polySplit3.out" "polySplit4.ip";
connectAttr "polySplit4.out" "polySplit5.ip";
connectAttr "polySplit5.out" "polySplit6.ip";
connectAttr "polyCube2.out" "polySplit7.ip";
connectAttr "polySplit7.out" "polySplit8.ip";
connectAttr "polySplit8.out" "polySplit9.ip";
connectAttr "polySplit9.out" "polySplit10.ip";
connectAttr "polySplit10.out" "polySplit11.ip";
connectAttr "polySplit11.out" "polyPlanarProj2.ip";
connectAttr "pCubeShape2.wm" "polyPlanarProj2.mp";
connectAttr "polyPlanarProj2.out" "polyMapCut4.ip";
connectAttr "polyMapCut4.out" "polyMapCut5.ip";
connectAttr "polyMapCut5.out" "polyTweakUV3.ip";
connectAttr "polyTweakUV3.out" "polyMapSew1.ip";
connectAttr "polyMapSew1.out" "polyTweakUV4.ip";
connectAttr "polySplit6.out" "polyTweakUV5.ip";
connectAttr "polyCube3.out" "polyPlanarProj3.ip";
connectAttr "pCubeShape4.wm" "polyPlanarProj3.mp";
connectAttr "polyPlanarProj3.out" "polyMapCut6.ip";
connectAttr "polyMapCut6.out" "polyMapCut7.ip";
connectAttr "polyMapCut7.out" "polyMapCut8.ip";
connectAttr "polyMapCut8.out" "polyMapCut9.ip";
connectAttr "polyMapCut9.out" "polyMapCut10.ip";
connectAttr "polyMapCut10.out" "polyTweakUV6.ip";
connectAttr "polyTweakUV6.out" "polyMapCut11.ip";
connectAttr "polyMapCut11.out" "polyTweakUV7.ip";
connectAttr "polyTweakUV5.out" "polyLayoutUV1.ip";
connectAttr "polyLayoutUV1.out" "polyTweakUV8.ip";
connectAttr "polyTweakUV8.out" "polyLayoutUV2.ip";
connectAttr ":defaultArnoldDenoiser.msg" ":defaultArnoldRenderOptions.imagers" -na
		;
connectAttr ":defaultArnoldDisplayDriver.msg" ":defaultArnoldRenderOptions.drivers"
		 -na;
connectAttr ":defaultArnoldFilter.msg" ":defaultArnoldRenderOptions.filt";
connectAttr ":defaultArnoldDriver.msg" ":defaultArnoldRenderOptions.drvr";
connectAttr "polySurfaceShape1.o" "polyLayoutUV3.ip";
connectAttr "polyTweakUV4.out" "polyLayoutUV4.ip";
connectAttr "polyLayoutUV3.out" "polyTweakUV9.ip";
connectAttr "polyLayoutUV4.out" "polyTweakUV10.ip";
connectAttr "polyTweakUV7.out" "polyLayoutUV5.ip";
connectAttr "polyLayoutUV5.out" "polyTweakUV11.ip";
connectAttr "defaultRenderLayer.msg" ":defaultRenderingList1.r" -na;
connectAttr "pCubeShape1.iog" ":initialShadingGroup.dsm" -na;
connectAttr "pCubeShape2.iog" ":initialShadingGroup.dsm" -na;
connectAttr "pCubeShape3.iog" ":initialShadingGroup.dsm" -na;
connectAttr "pCubeShape4.iog" ":initialShadingGroup.dsm" -na;
// End of Fuk.ma
