﻿/*
This file in the main entry point for defining grunt tasks and using grunt plugins.
Click here to learn more. http://go.microsoft.com/fwlink/?LinkID=513275&clcid=0x409
*/
module.exports = function (grunt) {
    var bowerConfig = {
        install: {
            options: {
                targetDir: "wwwroot/lib",
                layout: "byComponent",
                cleanTargetDir: false
            }
        }
    };

    grunt.initConfig({
        bower: bowerConfig
    });

    grunt.registerTask("default", [ "bower:install" ]);

    grunt.loadNpmTasks("grunt-bower-task");
};